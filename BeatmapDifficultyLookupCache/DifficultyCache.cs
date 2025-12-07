// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatmapDifficultyLookupCache.Models;
using Dapper;
using Microsoft.Extensions.Logging;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Server.QueueProcessor;

namespace BeatmapDifficultyLookupCache
{
    public class DifficultyCache
    {
        private static readonly List<Ruleset> available_rulesets = getRulesets();
        private static readonly DifficultyAttributes empty_attributes = new DifficultyAttributes(Array.Empty<Mod>(), -1);

        private readonly Dictionary<DifficultyRequest, Task<DifficultyAttributes>> attributesCache = new Dictionary<DifficultyRequest, Task<DifficultyAttributes>>();
        private readonly Dictionary<DifficultyRequest, Task<BeatmapStrains>> strainsCache = new Dictionary<DifficultyRequest, Task<BeatmapStrains>>();
        private readonly ILogger logger;

        public DifficultyCache(ILogger<DifficultyCache> logger)
        {
            this.logger = logger;
        }

        public async Task<double> GetDifficultyRating(DifficultyRequest request)
        {
            if (request.BeatmapId == 0)
                return 0;

            return (await computeAttributes(request)).StarRating;
        }

        public async Task<DifficultyAttributes> GetAttributes(DifficultyRequest request)
        {
            if (request.BeatmapId == 0)
                return empty_attributes;

            return await computeAttributes(request);
        }

        private async Task<DifficultyAttributes> computeAttributes(DifficultyRequest request)
        {
            Task<DifficultyAttributes>? task;

            lock (attributesCache)
            {
                if (!attributesCache.TryGetValue(request, out task))
                {
                    attributesCache[request] = task = Task.Run(async () =>
                    {
                        var apiMods = request.GetMods();

                        logger.LogInformation("Computing difficulty (beatmap: {BeatmapId}, ruleset: {RulesetId}, mods: {Mods})",
                            request.BeatmapId,
                            request.RulesetId,
                            apiMods.Select(m => m.ToString()));

                        try
                        {
                            var ruleset = available_rulesets.First(r => r.RulesetInfo.OnlineID == request.RulesetId);
                            var mods = apiMods.Select(m => m.ToMod(ruleset)).ToArray();
                            // Track how long it takes to get the beatmap
                            var beatmap = await getBeatmap(request.BeatmapId);

                            var difficultyCalculator = ruleset.CreateDifficultyCalculator(beatmap);
                            var attributes = difficultyCalculator.Calculate(mods);

                            // Trim a few members which we don't consume and only take up RAM.
                            // attributes.Mods = Array.Empty<Mod>();

                            return attributes;
                        }
                        catch (Exception e)
                        {
                            logger.LogWarning("Request failed with \"{Message}\"", e.Message);
                            return empty_attributes;
                        }
                    });
                }
            }

            return await task;
        }

        public async Task<BeatmapStrains> GetStrains(DifficultyRequest request)
        {
            Task<BeatmapStrains>? task;
            lock (strainsCache)
            {
                if (!strainsCache.TryGetValue(request, out task))
                {
                    strainsCache[request] = task = Task.Run(async () =>
                    {
                        var apiMods = request.GetMods();
                        logger.LogInformation("Computing strains (beatmap: {BeatmapId}, ruleset: {RulesetId}, mods: {Mods})",
                            request.BeatmapId,
                            request.RulesetId,
                            apiMods.Select(m => m.ToString()));
                        try
                        {
                            var ruleset = available_rulesets.First(r => r.RulesetInfo.OnlineID == request.RulesetId);
                            var mods = apiMods.Select(m => m.ToMod(ruleset)).ToArray();
                            var beatmap = await getBeatmap(request.BeatmapId);
                            //var difficultyCalculator = ruleset.CreateDifficultyCalculator(beatmap);
                            var difficultyCalculator = RulesetHelper.GetExtendedDifficultyCalculator(ruleset.RulesetInfo, beatmap);
                            difficultyCalculator.Calculate(mods); //forces to calculate clockrate
                            //var attributes = difficultyCalculator.Calculate(mods);
                            //var skills = difficultyCalculator.CreateSkills();

                            Skill[] skills = ((IExtendedDifficultyCalculator)difficultyCalculator).GetSkills();
                            var strainSkills = skills.Where(x => x is StrainSkill or StrainDecaySkill).ToArray();

                            BeatmapStrains strains = new BeatmapStrains(request.BeatmapId, request.Mods);

                            foreach (var skill in strainSkills)
                            {
                                double[] _strains = ((StrainSkill)skill).GetCurrentStrainPeaks().ToArray();
                                //get name of the skill object
                                string name = skill.GetType().Name;

                                strains.AddStrains(name, _strains);
                            }

                            return strains;
                        }
                        catch (Exception e)
                        {
                            logger.LogWarning("Request failed with \"{Message}\"", e.Message);
                            return new BeatmapStrains(request.BeatmapId, request.Mods);
                        }
                    });
                }
            }
            return await task;
        }
        public void Purge(int beatmapId)
        {
            logger.LogInformation("Purging (beatmap: {BeatmapId})", beatmapId);

            lock (attributesCache)
            {
                foreach (var req in attributesCache.Keys.ToArray())
                {
                    if (req.BeatmapId == beatmapId)
                        attributesCache.Remove(req);
                }
            }
        }

        private async Task<WorkingBeatmap> getBeatmap(int beatmapId)
        {
            //Check if a local file exists (./beatmaps/{beatmapId}.osu), at exe path
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "beatmaps", $"{beatmapId}.osu");

            if (File.Exists(localPath))
            {
                //If file is older than 1 week, we don't use it
                var fileInfo = new FileInfo(localPath);
                if (fileInfo.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-7))
                {
                    logger.LogInformation("Local beatmap file is older than 1 week, redownloading (beatmap: {BeatmapId})", beatmapId);
                }
                else
                {
                    logger.LogInformation("Using local beatmap file (beatmap: {BeatmapId})", beatmapId);
                    using (var fs = File.OpenRead(localPath))
                    {
                        LoaderWorkingBeatmap wb = new LoaderWorkingBeatmap(fs);
                        return wb;
                    }
                }
            }

            var req = new WebRequest(string.Format(Environment.GetEnvironmentVariable("DOWNLOAD_PATH") ?? "https://osu.ppy.sh/osu/{0}", beatmapId))
            {
                AllowInsecureRequests = true,
            };

            await req.PerformAsync();

            if (req.ResponseStream.Length == 0)
                throw new Exception($"Retrieved zero-length beatmap ({beatmapId})!");

            LoaderWorkingBeatmap workingBeatmap = new LoaderWorkingBeatmap(req.ResponseStream, false);

            // Cache the beatmap locally for future use
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                //if file exists, overwrite

                using (var fs = File.Create(localPath))
                {
                    req.ResponseStream.Seek(0, SeekOrigin.Begin);
                    await req.ResponseStream.CopyToAsync(fs);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to cache beatmap locally: {Message}", e.Message);
            }

            req.ResponseStream.Dispose();

            return workingBeatmap;
        }

        private static List<Ruleset> getRulesets()
        {
            const string ruleset_library_prefix = "osu.Game.Rulesets";

            var rulesetsToProcess = new List<Ruleset>();

            foreach (string file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, $"{ruleset_library_prefix}.*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    Type type = assembly.GetTypes().First(t => t.IsPublic && t.IsSubclassOf(typeof(Ruleset)));
                    rulesetsToProcess.Add((Ruleset)Activator.CreateInstance(type)!);
                }
                catch
                {
                    throw new Exception($"Failed to load ruleset ({file})");
                }
            }

            return rulesetsToProcess;
        }

        private static int getModBitwise(int rulesetId, List<APIMod> mods)
        {
            int val = 0;

            foreach (var mod in mods)
                val |= (int)getLegacyMod(mod);

            return val;

            LegacyMods getLegacyMod(APIMod mod)
            {
                switch (mod.Acronym)
                {
                    case "EZ":
                        return LegacyMods.Easy;

                    case "HR":
                        return LegacyMods.HardRock;

                    case "NC":
                        return LegacyMods.DoubleTime;

                    case "DT":
                        return LegacyMods.DoubleTime;

                    case "HT":
                        return LegacyMods.HalfTime;

                    case "4K":
                        return LegacyMods.Key4;

                    case "5K":
                        return LegacyMods.Key5;

                    case "6K":
                        return LegacyMods.Key6;

                    case "7K":
                        return LegacyMods.Key7;

                    case "8K":
                        return LegacyMods.Key8;

                    case "9K":
                        return LegacyMods.Key9;

                    case "FL" when rulesetId == 0:
                        return LegacyMods.Flashlight;

                    case "HD" when rulesetId == 0:
                        return LegacyMods.Hidden;

                    case "TD" when rulesetId == 0:
                        return LegacyMods.TouchDevice;
                }

                return 0;
            }
        }
    }
}

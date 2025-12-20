using BeatmapDifficultyLookupCache.Models;
using Microsoft.AspNetCore.Mvc;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Screens;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Replays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using osu.Game.Screens;
using osu.Game.Screens.Play;
using osuTK;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BeatmapDifficultyLookupCache.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExtraController
    {
        private readonly DifficultyCache cache;

        public ExtraController(DifficultyCache cache)
        {
            this.cache = cache;
        }

        [HttpPost] //returns a stream of the beatmap file corresponding to the given beatmap ID
        public async Task<ExtraResult> Post([FromBody] ExtraRequest request)
        {
            WorkingBeatmap? workingBeatmap = await cache.getBeatmap(request.BeatmapId);
            Ruleset ruleset;
            switch (request.RulesetId)
            {
                default:
                case 0:
                    ruleset = new OsuRuleset();
                    break;

                case 1:
                    ruleset = new TaikoRuleset();
                    break;

                case 2:
                    ruleset = new CatchRuleset();
                    break;

                case 3:
                    ruleset = new ManiaRuleset();
                    break;
            }

            var mods = request.GetMods().Select(m => m.ToMod(ruleset)).ToArray();
            IBeatmap beatmap = workingBeatmap.GetPlayableBeatmap(ruleset.RulesetInfo, mods);

            return new ExtraResult
            {
                beatmap_id = request.BeatmapId,
                mods = request.Mods,
                beatmap = beatmap,
            };
        }
    }
}

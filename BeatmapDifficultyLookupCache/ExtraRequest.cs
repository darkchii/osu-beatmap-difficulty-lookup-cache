using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Game.Online.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatmapDifficultyLookupCache
{
    public class ExtraRequest : IEquatable<ExtraRequest>
    {
        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; init; }

        [JsonProperty("ruleset_id")]
        public int RulesetId { get; init; }

        [JsonProperty("mods")]
        public JArray? Mods { get; init; }

        public bool Equals(ExtraRequest? other)
        {
            if (other is null)
                return false;
            return BeatmapId == other.BeatmapId &&
                   RulesetId == other.RulesetId &&
                   JToken.DeepEquals(Mods, other.Mods);
        }

        public List<APIMod> GetMods()
        {
            var apiMods = new List<APIMod>(Mods?.ToObject<APIMod[]>()?.OrderBy(m => m.Acronym).ToArray() ?? Array.Empty<APIMod>());

            // Hacks for some stable-specific mods.
            apiMods.RemoveAll(m =>
            {
                string? acronym = m.Acronym?.ToUpper();

                if (string.IsNullOrWhiteSpace(acronym))
                    return true;

                switch (acronym)
                {
                    case "SCOREV2":
                    case "CINEMA":
                    case "RELAX":
                    case "AUTO":
                        return true;
                }

                return false;
            });

            // Stable provides an unexpected acronym for dual stages.
            foreach (var m in apiMods)
            {
                if (m.Acronym == "2P")
                    m.Acronym = "DS";
            }

            return apiMods;
        }
    }
}

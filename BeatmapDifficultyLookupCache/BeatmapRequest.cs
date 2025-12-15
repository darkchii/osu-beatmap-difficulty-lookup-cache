using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using System;

namespace BeatmapDifficultyLookupCache
{
    public class BeatmapRequest : IEquatable<BeatmapRequest>
    {
        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; init; }

        public bool Equals(BeatmapRequest? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return BeatmapId == other.BeatmapId;
        }
    }
}

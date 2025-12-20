using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Replays;
using osu.Game.Scoring;

namespace BeatmapDifficultyLookupCache.Models
{
    [Serializable]
    public class ExtraResult
    {
        public int beatmap_id { get; set; }
        public JArray? mods { get; set; }
        public IBeatmap beatmap { get; set; }
    }
}

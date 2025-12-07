using Newtonsoft.Json;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;

namespace BeatmapDifficultyLookupCache.Models
{
    public interface IExtendedDifficultyAttributes
    {
        double FirstObjectStartTime { get; set; }
        double LastObjectEndTime { get; set; }
    }

    //Adds some extra data required for performance calculations which cant be fetched on-the-fly
    public class ExtendedOsuDifficultyAttributes : OsuDifficultyAttributes, IExtendedDifficultyAttributes
    {
        [JsonProperty("first_object_start_time")]
        public double FirstObjectStartTime { get; set; }

        [JsonProperty("last_object_end_time")]
        public double LastObjectEndTime { get; set; }

        public static ExtendedOsuDifficultyAttributes FromBase(OsuDifficultyAttributes baseAttributes)
        {
            return new ExtendedOsuDifficultyAttributes
            {
                Mods = baseAttributes.Mods,
                StarRating = baseAttributes.StarRating,
                MaxCombo = baseAttributes.MaxCombo,
                AimDifficulty = baseAttributes.AimDifficulty,
                AimDifficultSliderCount = baseAttributes.AimDifficultSliderCount,
                SpeedDifficulty = baseAttributes.SpeedDifficulty,
                SpeedNoteCount = baseAttributes.SpeedNoteCount,
                FlashlightDifficulty = baseAttributes.FlashlightDifficulty,
                SliderFactor = baseAttributes.SliderFactor,
                AimTopWeightedSliderFactor = baseAttributes.AimTopWeightedSliderFactor,
                SpeedTopWeightedSliderFactor = baseAttributes.SpeedTopWeightedSliderFactor,
                AimDifficultStrainCount = baseAttributes.AimDifficultStrainCount,
                SpeedDifficultStrainCount = baseAttributes.SpeedDifficultStrainCount,
                NestedScorePerObject = baseAttributes.NestedScorePerObject,
                LegacyScoreBaseMultiplier = baseAttributes.LegacyScoreBaseMultiplier,
                MaximumLegacyComboScore = baseAttributes.MaximumLegacyComboScore,

                DrainRate = baseAttributes.DrainRate,
                HitCircleCount = baseAttributes.HitCircleCount,
                SliderCount = baseAttributes.SliderCount,
                SpinnerCount = baseAttributes.SpinnerCount
            };
        }
    }

    public class  ExtendedTaikoDifficultyAttributes : TaikoDifficultyAttributes, IExtendedDifficultyAttributes
    {
        [JsonProperty("first_object_start_time")]
        public double FirstObjectStartTime { get; set; }
        [JsonProperty("last_object_end_time")]
        public double LastObjectEndTime { get; set; }

        public static ExtendedTaikoDifficultyAttributes FromBase(TaikoDifficultyAttributes baseAttributes)
        {
            return new ExtendedTaikoDifficultyAttributes
            {
                Mods = baseAttributes.Mods,
                StarRating = baseAttributes.StarRating,
                MaxCombo = baseAttributes.MaxCombo,
                MechanicalDifficulty = baseAttributes.MechanicalDifficulty,
                RhythmDifficulty = baseAttributes.RhythmDifficulty,
                ReadingDifficulty = baseAttributes.ReadingDifficulty,
                ColourDifficulty = baseAttributes.ColourDifficulty,
                StaminaDifficulty = baseAttributes.StaminaDifficulty,
                MonoStaminaFactor = baseAttributes.MonoStaminaFactor,
                ConsistencyFactor = baseAttributes.ConsistencyFactor,

                StaminaTopStrains = baseAttributes.StaminaTopStrains
            };
        }
    }

    public class ExtendedCatchDifficultyAttributes : CatchDifficultyAttributes, IExtendedDifficultyAttributes
    {
        [JsonProperty("first_object_start_time")]
        public double FirstObjectStartTime { get; set; }
        [JsonProperty("last_object_end_time")]
        public double LastObjectEndTime { get; set; }

        public static ExtendedCatchDifficultyAttributes FromBase(CatchDifficultyAttributes baseAttributes)
        {
            return new ExtendedCatchDifficultyAttributes
            {
                Mods = baseAttributes.Mods,
                StarRating = baseAttributes.StarRating,
                MaxCombo = baseAttributes.MaxCombo,
            };
        }
    }

    public class ExtendedManiaDifficultyAttributes : ManiaDifficultyAttributes, IExtendedDifficultyAttributes
    {
        [JsonProperty("first_object_start_time")]
        public double FirstObjectStartTime { get; set; }
        [JsonProperty("last_object_end_time")]
        public double LastObjectEndTime { get; set; }

        public static ExtendedManiaDifficultyAttributes FromBase(ManiaDifficultyAttributes baseAttributes)
        {
            return new ExtendedManiaDifficultyAttributes
            {
                Mods = baseAttributes.Mods,
                StarRating = baseAttributes.StarRating,
                MaxCombo = baseAttributes.MaxCombo,
            };
        }
    }
}

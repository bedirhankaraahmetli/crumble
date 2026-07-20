using System;
using BreakInfinity;

namespace Crumble.Numerics
{
    /// <summary>
    /// All balance formulas (GDD §9) live here — no other class may inline economy math.
    /// Everything runs on BigDouble; base values come from ScriptableObjects as doubles.
    /// </summary>
    public static class GameMath
    {
        /// <summary>Research can never discount upgrades by more than this (anti-degenerate cap).</summary>
        public const double MaxUpgradeCostReduction = 0.5;

        /// <summary>Applies the research upgrade-cost discount, clamped to the cap.</summary>
        public static BigDouble ApplyCostReduction(BigDouble baseCost, double reduction)
        {
            var clamped = Math.Max(0.0, Math.Min(reduction, MaxUpgradeCostReduction));
            return baseCost * (1.0 - clamped);
        }

        /// <summary>Cost = BaseCost × GrowthFactor^Level (GDD §9).</summary>
        public static BigDouble UpgradeCost(BigDouble baseCost, double growthFactor, int level)
        {
            return baseCost * BigDouble.Pow(growthFactor, level);
        }

        /// <summary>Total cost of the next <paramref name="count"/> levels (geometric series sum).</summary>
        public static BigDouble BulkUpgradeCost(BigDouble baseCost, double growthFactor, int currentLevel, int count)
        {
            if (count <= 0)
            {
                return BigDouble.Zero;
            }

            var first = UpgradeCost(baseCost, growthFactor, currentLevel);
            if (Math.Abs(growthFactor - 1.0) < 1e-12)
            {
                return first * count;
            }

            return first * (BigDouble.Pow(growthFactor, count) - 1) / (growthFactor - 1);
        }

        /// <summary>
        /// Highest number of consecutive levels affordable with <paramref name="budget"/>
        /// (0 if even the next level is too expensive). Closed-form via logs, then corrected
        /// for floating-point error so the result is exact against BulkUpgradeCost.
        /// </summary>
        public static int MaxAffordable(BigDouble baseCost, double growthFactor, int currentLevel, BigDouble budget)
        {
            var next = UpgradeCost(baseCost, growthFactor, currentLevel);
            if (budget < next)
            {
                return 0;
            }

            int count;
            if (Math.Abs(growthFactor - 1.0) < 1e-12)
            {
                var flat = BigDouble.Floor(budget / next).ToDouble();
                if (flat >= int.MaxValue)
                {
                    return int.MaxValue;
                }

                count = (int)flat;
            }
            else
            {
                // Invert the geometric sum: n = log_g(budget·(g−1)/first + 1)
                var n = BigDouble.Log10(budget * (growthFactor - 1) / next + 1) / Math.Log10(growthFactor);
                if (n >= int.MaxValue)
                {
                    return int.MaxValue;
                }

                count = (int)Math.Floor(n + 1e-9);
            }

            while (count > 0 && BulkUpgradeCost(baseCost, growthFactor, currentLevel, count) > budget)
            {
                count--;
            }

            while (count < int.MaxValue && BulkUpgradeCost(baseCost, growthFactor, currentLevel, count + 1) <= budget)
            {
                count++;
            }

            return count;
        }

        /// <summary>HP = BaseHP × DifficultyFactor^Stage (GDD §9).</summary>
        public static BigDouble TabletHp(BigDouble baseHp, double difficultyFactor, int stage)
        {
            return baseHp * BigDouble.Pow(difficultyFactor, stage);
        }

        /// <summary>
        /// Prestige reward (GDD §9): KP = floor(∛(lifetimeCoins / divisor)).
        /// The cube root keeps KP inflation flat; the divisor sets when the first KP appears.
        /// </summary>
        public static BigDouble PrestigeKnowledge(BigDouble lifetimeCoinsThisRun, BigDouble divisor)
        {
            if (lifetimeCoinsThisRun <= 0)
            {
                return BigDouble.Zero;
            }

            var kp = Cbrt(lifetimeCoinsThisRun / divisor);
            if (kp < 1)
            {
                return BigDouble.Zero;
            }

            // Absorb Pow() floating-point error so ∛1000 floors to 10, not 9.
            return BigDouble.Floor(kp * (1 + 1e-12));
        }

        public static BigDouble Cbrt(BigDouble value)
        {
            return BigDouble.Pow(value, 1.0 / 3.0);
        }

        /// <summary>Lump-sum coins for shattering a tablet (grows within a material like HP does).</summary>
        public static BigDouble TabletReward(BigDouble baseReward, double rewardGrowthFactor, int stageWithinMaterial)
        {
            return baseReward * BigDouble.Pow(rewardGrowthFactor, stageWithinMaterial);
        }

        /// <summary>Coin trickle per point of damage dealt (GDD §2: earn per tap / per tick).</summary>
        public static BigDouble CoinsForDamage(BigDouble damage, double coinPerDamageRatio)
        {
            return damage * coinPerDamageRatio;
        }

        /// <summary>
        /// Which material a global stage belongs to. Progression never runs out: past the
        /// last material, the index stays capped and stages keep scaling there.
        /// </summary>
        public static int MaterialIndexForStage(int stage, int stagesPerMaterial, int materialCount)
        {
            if (stage < 0 || stagesPerMaterial <= 0 || materialCount <= 0)
            {
                return 0;
            }

            var index = stage / stagesPerMaterial;
            return index >= materialCount ? materialCount - 1 : index;
        }

        /// <summary>
        /// Stage counted within the current material — the exponent for HP/reward scaling.
        /// Grows without bound on the final material so difficulty never plateaus.
        /// </summary>
        public static int StageWithinMaterial(int stage, int stagesPerMaterial, int materialCount)
        {
            if (stage < 0 || stagesPerMaterial <= 0 || materialCount <= 0)
            {
                return 0;
            }

            return stage - MaterialIndexForStage(stage, stagesPerMaterial, materialCount) * stagesPerMaterial;
        }

        /// <summary>
        /// True on a material's final stage — the milestone ("boss") tablet with extra HP
        /// and reward. On the endless last material this recurs every stagesPerMaterial
        /// stages so milestones never stop appearing.
        /// </summary>
        public static bool IsMilestoneStage(int stage, int stagesPerMaterial, int materialCount)
        {
            if (stage < 0 || stagesPerMaterial <= 0 || materialCount <= 0)
            {
                return false;
            }

            return StageWithinMaterial(stage, stagesPerMaterial, materialCount) % stagesPerMaterial
                   == stagesPerMaterial - 1;
        }

        /// <summary>Tablet HP including the milestone-stage multiplier.</summary>
        public static BigDouble TabletHp(
            BigDouble baseHp, double difficultyFactor, int stageWithinMaterial,
            bool isMilestone, double milestoneHpMultiplier)
        {
            var hp = TabletHp(baseHp, difficultyFactor, stageWithinMaterial);
            return isMilestone ? hp * milestoneHpMultiplier : hp;
        }

        /// <summary>Shatter reward including the milestone-stage multiplier.</summary>
        public static BigDouble TabletReward(
            BigDouble baseReward, double rewardGrowthFactor, int stageWithinMaterial,
            bool isMilestone, double milestoneRewardMultiplier)
        {
            var reward = TabletReward(baseReward, rewardGrowthFactor, stageWithinMaterial);
            return isMilestone ? reward * milestoneRewardMultiplier : reward;
        }
    }
}

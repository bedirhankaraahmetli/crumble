using BreakInfinity;
using Crumble.Numerics;
using NUnit.Framework;

namespace Crumble.Tests
{
    public sealed class GameMathTests
    {
        /// <summary>Relative comparison — absolute deltas are meaningless at idle-game scale.</summary>
        private static void AssertApprox(BigDouble expected, BigDouble actual, double relativeTolerance = 1e-9)
        {
            if (expected == 0)
            {
                Assert.That(actual.ToDouble(), Is.EqualTo(0).Within(relativeTolerance));
                return;
            }

            var ratio = (actual / expected).ToDouble();
            Assert.That(ratio, Is.EqualTo(1.0).Within(relativeTolerance),
                $"expected ~{expected}, got {actual}");
        }

        // ---- UpgradeCost ----

        [Test]
        public void UpgradeCost_AtLevelZero_IsBaseCost()
        {
            AssertApprox(10, GameMath.UpgradeCost(10, 1.07, 0));
        }

        [Test]
        public void UpgradeCost_GrowsGeometrically()
        {
            AssertApprox(10 * 1.07 * 1.07, GameMath.UpgradeCost(10, 1.07, 2));
        }

        // ---- BulkUpgradeCost ----

        [Test]
        public void BulkUpgradeCost_MatchesSummingIndividualLevels()
        {
            BigDouble sum = 0;
            for (var i = 0; i < 25; i++)
            {
                sum += GameMath.UpgradeCost(10, 1.15, 5 + i);
            }

            AssertApprox(sum, GameMath.BulkUpgradeCost(10, 1.15, 5, 25));
        }

        [Test]
        public void BulkUpgradeCost_ZeroCount_IsZero()
        {
            Assert.That(GameMath.BulkUpgradeCost(10, 1.15, 5, 0) == 0, Is.True);
        }

        // ---- MaxAffordable ----

        [Test]
        public void MaxAffordable_NextLevelTooExpensive_ReturnsZero()
        {
            Assert.That(GameMath.MaxAffordable(100, 1.07, 0, 99), Is.EqualTo(0));
        }

        [Test]
        public void MaxAffordable_ExactBudget_BuysExactCount()
        {
            var budget = GameMath.BulkUpgradeCost(10, 1.15, 3, 7);
            Assert.That(GameMath.MaxAffordable(10, 1.15, 3, budget), Is.EqualTo(7));
        }

        [Test]
        public void MaxAffordable_JustUnderExactBudget_BuysOneFewer()
        {
            var budget = GameMath.BulkUpgradeCost(10, 1.15, 3, 7) - 0.01;
            Assert.That(GameMath.MaxAffordable(10, 1.15, 3, budget), Is.EqualTo(6));
        }

        [Test]
        public void MaxAffordable_HugeBudget_IsConsistentWithBulkCost()
        {
            var budget = new BigDouble(1, 50); // 1e50 coins
            var n = GameMath.MaxAffordable(10, 1.07, 0, budget);

            Assert.That(n, Is.GreaterThan(0));
            Assert.That(GameMath.BulkUpgradeCost(10, 1.07, 0, n) <= budget, Is.True,
                "cost of n levels must fit the budget");
            Assert.That(GameMath.BulkUpgradeCost(10, 1.07, 0, n + 1) > budget, Is.True,
                "n+1 levels must exceed the budget");
        }

        // ---- TabletHp ----

        [Test]
        public void TabletHp_StageZero_IsBaseHp()
        {
            AssertApprox(100, GameMath.TabletHp(100, 1.6, 0));
        }

        [Test]
        public void TabletHp_ScalesExponentially()
        {
            AssertApprox(100 * BigDouble.Pow(1.6, 12), GameMath.TabletHp(100, 1.6, 12));
        }

        // ---- PrestigeKnowledge ----

        [Test]
        public void PrestigeKnowledge_PerfectCube_IsExact()
        {
            // 1e12 / 1e9 = 1000, ∛1000 = 10 — must not floor to 9 via fp error.
            var kp = GameMath.PrestigeKnowledge(new BigDouble(1, 12), new BigDouble(1, 9));
            Assert.That(kp.ToDouble(), Is.EqualTo(10).Within(1e-9));
        }

        [Test]
        public void PrestigeKnowledge_BelowThreshold_IsZero()
        {
            var kp = GameMath.PrestigeKnowledge(5e8, 1e9); // ratio 0.5 → ∛ < 1
            Assert.That(kp.ToDouble(), Is.EqualTo(0));
        }

        [Test]
        public void PrestigeKnowledge_ExactlyAtDivisor_IsOne()
        {
            var kp = GameMath.PrestigeKnowledge(new BigDouble(1, 9), new BigDouble(1, 9));
            Assert.That(kp.ToDouble(), Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void PrestigeKnowledge_ZeroCoins_IsZero()
        {
            Assert.That(GameMath.PrestigeKnowledge(0, 1e9).ToDouble(), Is.EqualTo(0));
        }

        [Test]
        public void PrestigeKnowledge_IsMonotonic()
        {
            var divisor = new BigDouble(1, 9);
            var a = GameMath.PrestigeKnowledge(new BigDouble(2, 15), divisor);
            var b = GameMath.PrestigeKnowledge(new BigDouble(3, 15), divisor);
            Assert.That(b >= a, Is.True);
        }

        // ---- ApplyCostReduction ----

        [Test]
        public void ApplyCostReduction_DiscountsProportionally()
        {
            AssertApprox(90, GameMath.ApplyCostReduction(100, 0.1));
        }

        [Test]
        public void ApplyCostReduction_ClampsAtTheCap()
        {
            AssertApprox(50, GameMath.ApplyCostReduction(100, 0.9)); // capped at 50%
        }

        [Test]
        public void ApplyCostReduction_NegativeReduction_IsIgnored()
        {
            AssertApprox(100, GameMath.ApplyCostReduction(100, -0.25));
        }

        // ---- Offline progress ----

        [Test]
        public void CappedOfflineSeconds_ClampsToTheCap()
        {
            Assert.That(GameMath.CappedOfflineSeconds(10 * 3600, 2), Is.EqualTo(2 * 3600));
            Assert.That(GameMath.CappedOfflineSeconds(600, 2), Is.EqualTo(600));
            Assert.That(GameMath.CappedOfflineSeconds(-50, 2), Is.EqualTo(0));
        }

        [Test]
        public void OfflineCoins_TrickplePlusAmortizedShatters()
        {
            // 10 DPS × 3600s = 36000 damage; trickle 36000×0.02 = 720;
            // shatters 36000/100 HP × 5 reward = 1800; ×0.5 efficiency = 1260
            var coins = GameMath.OfflineCoins(10, 3600, 0.02, 100, 5, 0.5);
            AssertApprox(1260, coins);
        }

        [Test]
        public void OfflineCoins_ScalesWithEfficiency()
        {
            var half = GameMath.OfflineCoins(10, 3600, 0.02, 100, 5, 0.5);
            var full = GameMath.OfflineCoins(10, 3600, 0.02, 100, 5, 1.0);
            AssertApprox(2, full / half);
        }

        [Test]
        public void OfflineCoins_NoDpsOrTime_IsZero()
        {
            Assert.That(GameMath.OfflineCoins(0, 3600, 0.02, 100, 5, 0.5) == 0, Is.True);
            Assert.That(GameMath.OfflineCoins(10, 0, 0.02, 100, 5, 0.5) == 0, Is.True);
        }

        // ---- Expeditions & events ----

        [Test]
        public void ExpeditionDuration_ShrinksWithSpeedResearch()
        {
            Assert.That(GameMath.ExpeditionDurationHours(4, 0), Is.EqualTo(4).Within(1e-9));
            Assert.That(GameMath.ExpeditionDurationHours(4, 1.0), Is.EqualTo(2).Within(1e-9));
            Assert.That(GameMath.ExpeditionDurationHours(4, -5), Is.EqualTo(4).Within(1e-9), "negative bonus ignored");
        }

        [Test]
        public void EventReward_UsesDpsIncomeWhenLarger()
        {
            AssertApprox(36000, GameMath.EventReward(10, 3600, 5, 25)); // 10 dps × 1h beats 125 floor
        }

        [Test]
        public void EventReward_FallsBackToTabletFloor()
        {
            AssertApprox(125, GameMath.EventReward(0, 3600, 5, 25)); // no assistants → 25 shatters
        }
    }
}

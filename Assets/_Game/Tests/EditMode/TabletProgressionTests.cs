using BreakInfinity;
using Crumble.Numerics;
using NUnit.Framework;

namespace Crumble.Tests
{
    public sealed class TabletProgressionTests
    {
        // ---- MaterialIndexForStage ----

        [TestCase(0, 0)]
        [TestCase(9, 0)]
        [TestCase(10, 1)]
        [TestCase(19, 1)]
        [TestCase(39, 3)]
        public void MaterialIndex_AdvancesEveryTenStages(int stage, int expectedIndex)
        {
            Assert.That(GameMath.MaterialIndexForStage(stage, 10, 4), Is.EqualTo(expectedIndex));
        }

        [Test]
        public void MaterialIndex_CapsAtLastMaterial()
        {
            Assert.That(GameMath.MaterialIndexForStage(999, 10, 4), Is.EqualTo(3));
        }

        // ---- StageWithinMaterial ----

        [TestCase(0, 0)]
        [TestCase(9, 9)]
        [TestCase(10, 0)]
        [TestCase(25, 5)]
        public void StageWithinMaterial_ResetsPerMaterial(int stage, int expected)
        {
            Assert.That(GameMath.StageWithinMaterial(stage, 10, 4), Is.EqualTo(expected));
        }

        [Test]
        public void StageWithinMaterial_KeepsGrowingOnFinalMaterial()
        {
            // stage 50 with 4 materials × 10 stages → capped at material 3, local stage 20
            Assert.That(GameMath.StageWithinMaterial(50, 10, 4), Is.EqualTo(20));
        }

        [Test]
        public void HpNeverPlateaus_PastTheLastMaterial()
        {
            BigDouble HpAt(int stage) => GameMath.TabletHp(
                18000, 1.5, GameMath.StageWithinMaterial(stage, 10, 4));

            Assert.That(HpAt(60) > HpAt(50), Is.True, "HP must keep scaling on the final material");
        }

        // ---- TabletReward / CoinsForDamage ----

        [Test]
        public void TabletReward_GrowsWithinMaterial()
        {
            var early = GameMath.TabletReward(5, 1.4, 0);
            var late = GameMath.TabletReward(5, 1.4, 9);

            Assert.That(early.ToDouble(), Is.EqualTo(5).Within(1e-9));
            Assert.That(late.ToDouble(), Is.EqualTo(5 * System.Math.Pow(1.4, 9)).Within(1e-6));
        }

        [Test]
        public void CoinsForDamage_IsProportional()
        {
            var coins = GameMath.CoinsForDamage(new BigDouble(500), 0.02);
            Assert.That(coins.ToDouble(), Is.EqualTo(10).Within(1e-9));
        }

        // ---- Milestone ("boss") stages ----

        [TestCase(8, false)]
        [TestCase(9, true)]   // last stage of material 0
        [TestCase(10, false)]
        [TestCase(19, true)]  // last stage of material 1
        [TestCase(39, true)]  // last stage of the final material
        [TestCase(45, false)]
        [TestCase(49, true)]  // recurs every 10 local stages on the endless final material
        public void MilestoneStage_IsEveryMaterialsFinalStage(int stage, bool expected)
        {
            Assert.That(GameMath.IsMilestoneStage(stage, 10, 4), Is.EqualTo(expected));
        }

        [Test]
        public void MilestoneHp_AppliesMultiplierOnlyOnMilestone()
        {
            var normal = GameMath.TabletHp(10, 1.5, 9, false, 2.0);
            var milestone = GameMath.TabletHp(10, 1.5, 9, true, 2.0);

            Assert.That((milestone / normal).ToDouble(), Is.EqualTo(2.0).Within(1e-9));
            Assert.That(normal.ToDouble(), Is.EqualTo(10 * System.Math.Pow(1.5, 9)).Within(1e-6));
        }

        [Test]
        public void MilestoneReward_AppliesMultiplierOnlyOnMilestone()
        {
            var normal = GameMath.TabletReward(5, 1.4, 9, false, 3.0);
            var milestone = GameMath.TabletReward(5, 1.4, 9, true, 3.0);

            Assert.That((milestone / normal).ToDouble(), Is.EqualTo(3.0).Within(1e-9));
        }
    }
}

using BreakInfinity;
using Crumble.Core;
using Crumble.Numerics;
using NUnit.Framework;

namespace Crumble.Tests
{
    /// <summary>Step 9: Time Crystal math, Cosmic Altar compounding, and cosmic save state.</summary>
    public sealed class CosmicEndgameTests
    {
        // ---- TimeCrystalsForArchive ----

        [Test]
        public void TimeCrystals_ZeroKnowledge_IsZero()
        {
            Assert.That(GameMath.TimeCrystalsForArchive(0, 1500) == 0, Is.True);
        }

        [Test]
        public void TimeCrystals_BelowDivisor_IsZero()
        {
            Assert.That(GameMath.TimeCrystalsForArchive(1000, 1500) == 0, Is.True);
        }

        [Test]
        public void TimeCrystals_AtDivisor_IsOne()
        {
            Assert.That(GameMath.TimeCrystalsForArchive(1500, 1500) == 1, Is.True);
        }

        [Test]
        public void TimeCrystals_HundredTimesDivisor_IsExactlyTen()
        {
            // √100 must floor to 10, not 9 — the fp-error guard at work
            Assert.That(GameMath.TimeCrystalsForArchive(150000, 1500) == 10, Is.True);
        }

        [Test]
        public void TimeCrystals_GrowSlowerThanKnowledge()
        {
            // perfect squares so flooring doesn't distort the ratio: √100 = 10, √10000 = 100
            var atX = GameMath.TimeCrystalsForArchive(150_000, 1500);
            var atHundredX = GameMath.TimeCrystalsForArchive(15_000_000, 1500);
            Assert.That((atHundredX / atX).ToDouble(), Is.EqualTo(10.0).Within(1e-9),
                "100× the KP should pay only 10× the crystals (square root)");
        }

        // ---- AltarMultiplier ----

        [Test]
        public void AltarMultiplier_LevelZero_IsOne()
        {
            Assert.That(GameMath.AltarMultiplier(1.5, 0) == 1, Is.True);
        }

        [Test]
        public void AltarMultiplier_CompoundsGeometrically()
        {
            Assert.That(GameMath.AltarMultiplier(1.5, 3).ToDouble(), Is.EqualTo(3.375).Within(1e-9));
        }

        // ---- ApplyKnowledgeMultiplier ----

        [Test]
        public void KnowledgeMultiplier_FloorsTheProduct()
        {
            Assert.That(GameMath.ApplyKnowledgeMultiplier(10, 1.25) == 12, Is.True);
        }

        [Test]
        public void KnowledgeMultiplier_ZeroBase_StaysZero()
        {
            Assert.That(GameMath.ApplyKnowledgeMultiplier(0, 5) == 0, Is.True);
        }

        [Test]
        public void KnowledgeMultiplier_IdentityMultiplier_KeepsValue()
        {
            Assert.That(GameMath.ApplyKnowledgeMultiplier(7, 1) == 7, Is.True);
        }

        // ---- cosmic save state ----

        [Test]
        public void CosmicState_RoundTripsThroughJson()
        {
            var data = new SaveData();
            data.Currencies.TimeCrystals = 42;
            data.Cosmic.ArchiveCount = 2;
            data.Cosmic.AltarUpgrades["altar_chrono_hammer"] = 3;

            var loaded = SaveSystem.FromJson(SaveSystem.ToJson(data));

            Assert.That(loaded.Currencies.TimeCrystals == 42, Is.True);
            Assert.That(loaded.Cosmic.ArchiveCount, Is.EqualTo(2));
            Assert.That(loaded.Cosmic.AltarUpgrades["altar_chrono_hammer"], Is.EqualTo(3));
        }

        [Test]
        public void LegacySave_WithoutCosmicBlock_LoadsDefaults()
        {
            var loaded = SaveSystem.FromJson("{\"version\":1}");

            Assert.That(loaded.Cosmic, Is.Not.Null, "pre-Step-9 saves must keep loading");
            Assert.That(loaded.Cosmic.ArchiveCount, Is.EqualTo(0));
            Assert.That(loaded.Cosmic.AltarUpgrades, Is.Empty);
        }
    }
}

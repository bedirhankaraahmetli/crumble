using System;
using System.IO;
using BreakInfinity;
using Crumble.Core;
using NUnit.Framework;

namespace Crumble.Tests
{
    public sealed class SaveSystemTests
    {
        private string _dir;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "CrumbleSaveTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        private static SaveData MakeSample()
        {
            var data = new SaveData();
            data.Currencies.AntiqueCoins = new BigDouble(1.2345678901234567, 456);
            data.Currencies.KnowledgePoints = 42;
            data.Currencies.LifetimeCoinsThisRun = new BigDouble(9.87654321, 1234);
            data.CurrentExcavation.TabletId = "tablet_obsidian";
            data.CurrentExcavation.Stage = 17;
            data.CurrentExcavation.RemainingHp = new BigDouble(5.5, 60);
            data.Upgrades.Tools["tool_dusting_brush"] = 25;
            data.Upgrades.Assistants["assistant_water_dripper"] = 12;
            data.ResearchTree["research_sharper_brushes"] = 3;
            data.LastLoginUnixUtc = 1700000000;
            return data;
        }

        private static void AssertBigDoubleExact(BigDouble expected, BigDouble actual)
        {
            Assert.That(actual.Mantissa, Is.EqualTo(expected.Mantissa), "mantissa must round-trip bit-exact");
            Assert.That(actual.Exponent, Is.EqualTo(expected.Exponent), "exponent must round-trip exactly");
        }

        // ---- JSON round-trip ----

        [Test]
        public void RoundTrip_PreservesBigDoublePrecision_BeyondDoubleRange()
        {
            var data = MakeSample();
            var loaded = SaveSystem.FromJson(SaveSystem.ToJson(data));

            AssertBigDoubleExact(data.Currencies.AntiqueCoins, loaded.Currencies.AntiqueCoins);
            AssertBigDoubleExact(data.Currencies.LifetimeCoinsThisRun, loaded.Currencies.LifetimeCoinsThisRun);
            AssertBigDoubleExact(data.CurrentExcavation.RemainingHp, loaded.CurrentExcavation.RemainingHp);
        }

        [Test]
        public void RoundTrip_PreservesStateDictionariesAndScalars()
        {
            var loaded = SaveSystem.FromJson(SaveSystem.ToJson(MakeSample()));

            Assert.That(loaded.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.LastLoginUnixUtc, Is.EqualTo(1700000000));
            Assert.That(loaded.CurrentExcavation.TabletId, Is.EqualTo("tablet_obsidian"));
            Assert.That(loaded.CurrentExcavation.Stage, Is.EqualTo(17));
            Assert.That(loaded.Upgrades.Tools["tool_dusting_brush"], Is.EqualTo(25));
            Assert.That(loaded.Upgrades.Assistants["assistant_water_dripper"], Is.EqualTo(12));
            Assert.That(loaded.ResearchTree["research_sharper_brushes"], Is.EqualTo(3));
        }

        [Test]
        public void FromJson_EmptyObject_YieldsUsableDefaults()
        {
            var loaded = SaveSystem.FromJson("{}");

            Assert.That(loaded.Currencies, Is.Not.Null);
            Assert.That(loaded.ResearchTree, Is.Not.Null);
            Assert.That(loaded.Currencies.AntiqueCoins == 0, Is.True);
        }

        [Test]
        public void FromJson_UnknownFutureFields_AreIgnored()
        {
            var loaded = SaveSystem.FromJson(
                "{\"version\":1,\"some_future_field\":true,\"currencies\":{\"antique_coins\":\"2e10\",\"another_new_thing\":5}}");

            Assert.That(loaded.Currencies.AntiqueCoins.ToDouble(), Is.EqualTo(2e10));
        }

        [Test]
        public void FromJson_PlainNumericCurrency_IsTolerated()
        {
            var loaded = SaveSystem.FromJson("{\"currencies\":{\"antique_coins\":12345}}");

            Assert.That(loaded.Currencies.AntiqueCoins.ToDouble(), Is.EqualTo(12345));
        }

        // ---- File IO ----

        [Test]
        public void WriteThenRead_RoundTripsThroughDisk()
        {
            SaveSystem.Write(MakeSample(), _path);
            var loaded = SaveSystem.Read(_path);

            Assert.That(loaded, Is.Not.Null);
            AssertBigDoubleExact(MakeSample().Currencies.AntiqueCoins, loaded.Currencies.AntiqueCoins);
        }

        [Test]
        public void Read_NoFile_ReturnsNull()
        {
            Assert.That(SaveSystem.Read(_path), Is.Null);
        }

        [Test]
        public void SecondWrite_KeepsPreviousSaveAsBackup()
        {
            var v1 = MakeSample();
            v1.CurrentExcavation.Stage = 1;
            SaveSystem.Write(v1, _path);

            var v2 = MakeSample();
            v2.CurrentExcavation.Stage = 2;
            SaveSystem.Write(v2, _path);

            Assert.That(File.Exists(_path + ".bak"), Is.True);
            Assert.That(SaveSystem.Read(_path).CurrentExcavation.Stage, Is.EqualTo(2));
        }

        [Test]
        public void Read_CorruptMainFile_FallsBackToBackup()
        {
            var v1 = MakeSample();
            v1.CurrentExcavation.Stage = 1;
            SaveSystem.Write(v1, _path);

            var v2 = MakeSample();
            v2.CurrentExcavation.Stage = 2;
            SaveSystem.Write(v2, _path);

            File.WriteAllText(_path, "{ this is not valid json !!");

            var loaded = SaveSystem.Read(_path);
            Assert.That(loaded, Is.Not.Null, "backup must be readable");
            Assert.That(loaded.CurrentExcavation.Stage, Is.EqualTo(1), "backup holds the previous save");
        }

        [Test]
        public void Write_NoTmpFileLeftBehind()
        {
            SaveSystem.Write(MakeSample(), _path);
            Assert.That(File.Exists(_path + ".tmp"), Is.False);
        }

        [Test]
        public void Delete_RemovesSaveAndBackup()
        {
            SaveSystem.Write(MakeSample(), _path);
            SaveSystem.Write(MakeSample(), _path); // second write creates the .bak

            SaveSystem.Delete(_path);

            Assert.That(File.Exists(_path), Is.False);
            Assert.That(File.Exists(_path + ".bak"), Is.False);
            Assert.That(SaveSystem.Read(_path), Is.Null);
        }

        [Test]
        public void Delete_MissingFiles_IsANoOp()
        {
            Assert.DoesNotThrow(() => SaveSystem.Delete(_path));
        }
    }
}

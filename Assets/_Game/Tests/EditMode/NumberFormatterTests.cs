using BreakInfinity;
using Crumble.Numerics;
using NUnit.Framework;

namespace Crumble.Tests
{
    public sealed class NumberFormatterTests
    {
        [TestCase(0, "0")]
        [TestCase(7, "7")]
        [TestCase(999, "999")]
        [TestCase(12.5, "12.5")]
        [TestCase(1234, "1.23K")]
        [TestCase(12345, "12.3K")]
        [TestCase(123456, "123K")]
        [TestCase(1500000, "1.50M")]
        [TestCase(1e9, "1.00B")]
        [TestCase(1e12, "1.00T")]
        [TestCase(1e15, "1.00aa")]
        [TestCase(1e18, "1.00ab")]
        [TestCase(-1234, "-1.23K")]
        public void Format_SmallAndNamedSuffixes(double value, string expected)
        {
            Assert.That(NumberFormatter.Format(value), Is.EqualTo(expected));
        }

        [Test]
        public void Format_SecondLetterCycleRollsOver()
        {
            // group 31 → letter index 26 → "ba"
            Assert.That(NumberFormatter.Format(new BigDouble(1, 93)), Is.EqualTo("1.00ba"));
        }

        [Test]
        public void Format_BeyondLetterPairs_FallsBackToScientific()
        {
            Assert.That(NumberFormatter.Format(new BigDouble(1.23, 3000)), Is.EqualTo("1.23e3000"));
        }

        [Test]
        public void Format_RoundingRollsIntoNextSuffix()
        {
            // 999,999 must not display as "1000K".
            Assert.That(NumberFormatter.Format(999999), Is.EqualTo("1.00M"));
        }

        [Test]
        public void Format_ThreeSignificantDigits_AtEveryMagnitude()
        {
            // 9.876e20 = 987.6 × 1e18 → "ab" range, 3 significant digits
            Assert.That(NumberFormatter.Format(new BigDouble(9.876, 20)), Is.EqualTo("988ab"));
        }
    }
}

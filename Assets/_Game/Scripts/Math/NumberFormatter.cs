using System.Globalization;
using BreakInfinity;

namespace Crumble.Numerics
{
    /// <summary>
    /// Short-form display for idle numbers, always 3 significant digits past 1000:
    /// 999 → "999", 1234 → "1.23K", 12345 → "12.3K", 1e6 → "1.00M", 1e15 → "1.00aa",
    /// 1e18 → "1.00ab" … and scientific notation ("1.23e3000") once letter pairs run out.
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] NamedSuffixes = { "", "K", "M", "B", "T" };

        // aa..zz after T: aa = 1e15, each following pair ×1000.
        private const int LetterPairCount = 26 * 26;

        public static string Format(BigDouble value)
        {
            if (double.IsNaN(value.Mantissa))
            {
                return "NaN";
            }

            if (value < 0)
            {
                return "-" + Format(-value);
            }

            if (value == 0)
            {
                return "0";
            }

            if (double.IsInfinity(value.Mantissa))
            {
                return "∞";
            }

            if (value < 1000)
            {
                var d = value.ToDouble();
                var format = d == System.Math.Floor(d) ? "0" : "0.##";
                return d.ToString(format, CultureInfo.InvariantCulture);
            }

            // Normalized BigDouble: Mantissa ∈ [1, 10), Exponent = floor(log10).
            var group = value.Exponent / 3;
            var scaled = value.Mantissa * System.Math.Pow(10, value.Exponent - group * 3); // ∈ [1, 1000)

            // Rounding can push 999.6 → "1000"; roll into the next suffix instead.
            if (scaled >= 999.5)
            {
                scaled /= 1000.0;
                group++;
            }

            string suffix;
            if (group < NamedSuffixes.Length)
            {
                suffix = NamedSuffixes[group];
            }
            else
            {
                var index = group - NamedSuffixes.Length;
                if (index >= LetterPairCount)
                {
                    return value.Mantissa.ToString("0.##", CultureInfo.InvariantCulture)
                           + "e" + value.Exponent.ToString(CultureInfo.InvariantCulture);
                }

                suffix = string.Concat((char)('a' + index / 26), (char)('a' + index % 26));
            }

            var digits = scaled < 10 ? "0.00" : scaled < 100 ? "0.0" : "0";
            return scaled.ToString(digits, CultureInfo.InvariantCulture) + suffix;
        }
    }
}

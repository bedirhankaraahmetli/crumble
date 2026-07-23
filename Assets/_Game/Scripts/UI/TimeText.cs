using System;

namespace Crumble.UI
{
    /// <summary>Short human duration: "2h 5m", "13m 4s", "45s".</summary>
    public static class TimeText
    {
        public static string Format(double seconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (span.TotalHours >= 1)
            {
                return (int)span.TotalHours + "h " + span.Minutes + "m";
            }

            return span.TotalMinutes >= 1 ? span.Minutes + "m " + span.Seconds + "s" : span.Seconds + "s";
        }
    }
}

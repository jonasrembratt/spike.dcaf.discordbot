using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DCAF.DiscordBot._lib
{
    public static class DateTimeHelper
    {
        static readonly Regex TimeSpanRegex = new Regex(@"(?<num>[\d\,\.]*)(?<unit>[d,h,m,s,ms]?)", RegexOptions.Compiled);

        public static class Units
        {
            public const string Days = "d";
            public const string Hours = "h";
            public const string Minutes = "m";
            public const string Seconds = "s";
            public const string Milliseconds = "ms";
        }
        
        public static bool TryParseTimeSpan(this string stringValue, string defaultUnit, out TimeSpan timeSpan, CultureInfo? cultureInfo = null)
        {
            timeSpan = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(stringValue))
                return false;

            var match = TimeSpanRegex.Match(stringValue);
            if (!match.Success)
                return false;

            cultureInfo ??= CultureInfo.InvariantCulture;
            var numeric = match.Groups["num"].Value;
            var unit = match.Groups["unit"].Value ?? defaultUnit;
            if (!double.TryParse(numeric, NumberStyles.Float, cultureInfo, out var dValue))
                return false;

            switch (unit)
            {
                case Units.Days:
                    timeSpan = TimeSpan.FromDays(dValue);
                    return true;

                case Units.Hours:
                    timeSpan = TimeSpan.FromHours(dValue);
                    return true;

                case Units.Minutes:
                    timeSpan = TimeSpan.FromMinutes(dValue);
                    return true;
           
                case Units.Seconds:
                    timeSpan = TimeSpan.FromSeconds(dValue);
                    return true;

                case Units.Milliseconds:
                    timeSpan = TimeSpan.FromMilliseconds(dValue);
                    return true;
                
                default: return false;
            }
        }

        public static TimeFrame Subtract(this TimeFrame self, TimeSpan timeSpan) => new TimeFrame(self.From.Subtract(timeSpan), self.To.Subtract(timeSpan));
    }
}
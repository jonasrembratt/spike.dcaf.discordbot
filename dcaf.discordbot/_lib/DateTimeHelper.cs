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
            public const string DaysIdent = "d";
            public const string HoursIdent = "h";
            public const string MinutesIdent = "m";
            public const string SecondsIdent = "s";
            public const string MillisecondsIdent = "ms";
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
                case Units.DaysIdent:
                    timeSpan = TimeSpan.FromDays(dValue);
                    return true;

                case Units.HoursIdent:
                    timeSpan = TimeSpan.FromHours(dValue);
                    return true;

                case Units.MinutesIdent:
                    timeSpan = TimeSpan.FromMinutes(dValue);
                    return true;
           
                case Units.SecondsIdent:
                    timeSpan = TimeSpan.FromSeconds(dValue);
                    return true;

                case Units.MillisecondsIdent:
                    timeSpan = TimeSpan.FromMilliseconds(dValue);
                    return true;
                
                default: return false;
            }
        }

        public static TimeFrame Subtract(this TimeFrame self, TimeSpan timeSpan) => new TimeFrame(self.From.Subtract(timeSpan), self.To.Subtract(timeSpan));
    }
}
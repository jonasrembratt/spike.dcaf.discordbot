using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using TetraPak.XP;

namespace DCAF._lib
{
    public static class CliHelper
    {
        // public const char CliValueQualifier = '=';
        public const string KeyFrom = "--from";
        public const string KeyTo = "--to";
        public const string KeyFromDate = "--from-date";
        public const string KeyToDate = "--to-date";
        const string DateTimeFormatIso1 = "yyyy-MM-ddTHH:mm"; 
        const string DateTimeFormatIso2 = "yyyy-M-dTHH:mm";
        const string DateTimeFormatIso3 = "yyyy-MM-dd"; 
        const string DateTimeFormatIso4 = "yyyy-M-d";
        const string DateTimeFormatGoogle1 = "M/d/yyyy H:mm:ss";
        // const string DateTimeFormatGoogle2 = "M/d/yyyy HH:mm:ss"; obsolete 
        static readonly string[] DateTimeIsoFormats = { DateTimeFormatIso1, DateTimeFormatIso2, DateTimeFormatIso3, DateTimeFormatIso4 };
        static readonly string[] DateTimeGoogleFormats = { DateTimeFormatGoogle1 };

        public static Outcome<TimeFrame> GetTimeFrame(this string[] args, TimeFrame? defaultTimeFrame)
        {
            DateTime? from = null;
            DateTime? to = null;

            if (args.TryGetValue(out var stringValue, KeyFrom, KeyFromDate))
            {
                 
                if (!DateTime.TryParseExact(stringValue, DateTimeIsoFormats, null, DateTimeStyles.None, out var dt))
                    return Outcome<TimeFrame>.Fail(new Exception($"The 'from' value is invalid: {stringValue}"));

                from = dt;
            }
            if (args.TryGetValue(out stringValue, KeyTo, KeyToDate))
            {
                if (!DateTime.TryParseExact(stringValue, DateTimeIsoFormats, null, DateTimeStyles.None, out var dt))
                    return Outcome<TimeFrame>.Fail(new Exception($"The 'to' value is invalid: {stringValue}"));

                to = dt;
            }
            
            from ??= defaultTimeFrame?.From;
            to ??= defaultTimeFrame?.To;
            return from is null && to is null
                ? Outcome<TimeFrame>.Fail(new Exception("No time frame specified"))
                : Outcome<TimeFrame>.Success(new TimeFrame(from!.Value, to!.Value));
        }

        public static bool TryParseGoogleDateTime(this string self, out DateTime value)
        {
            return DateTime.TryParseExact(self, DateTimeGoogleFormats, null, DateTimeStyles.None, out value);
        }
        
        public static bool TryGetValue(
            this string[] args,
#if NET5_0_OR_GREATER     
            [NotNullWhen(true)]
#endif            
            out string? value,
            params string[] keys)
        {
            for (var i = 0; i < args.Length-1; i++)
            {
                var key = args[i];
                if (keys.Length == 1 && keys[0] == key || keys.Any(i => i == key))
                {
                    value = args[i + 1];
                    return true;
                }
            }

            value = null;
            return false;
        }
        
        public static bool TryGetFlag(this string[] args, params string[] keys) 
            => 
            args.Any(key => keys.Any(i => i == key));
    }
}
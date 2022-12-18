using System;
using System.Collections.Generic;
using System.Globalization;
using DCAF._lib;
using Microsoft.Extensions.DependencyInjection;
using TetraPak.XP;
using TetraPak.XP.Configuration;

namespace DCAF.Discord.Scheduling;

public static class SchedulerHelper
{
    public static IServiceCollection AddScheduler(this IServiceCollection collection)
    {
        Configure.InsertValueParser(parseWeekdays);
        Configure.InsertValueParser(parseDateTime);
        Configure.InsertValueParser(parseDateTimeArray);
        Configure.InsertValueParser(parseTimeSpan);
        collection.AddSingleton<Scheduler>();
        return collection;
    }
        
    static bool parseWeekdays(string? stringValue, Type targetType, out object? value, object useDefault)
    {
        if (targetType != typeof(WeekdaysCollection))
        {
            value = useDefault;
            return false;
        }

        try
        {
            value = string.IsNullOrEmpty(stringValue)
                ? null
                : new WeekdaysCollection(stringValue);
            return value is { };
        }
        catch 
        {
            value = useDefault;
            return false;
        }
    }

    static bool parseDateTimeArray(string? stringValue, Type targetType, out object? value, object useDefault)
    {
        if (targetType != typeof(DateTime[]) || string.IsNullOrEmpty(stringValue))
        {
            value = useDefault;
            return false;
        }

        var times = new List<DateTime>();
        var split = stringValue.SplitAndTrim(new[] { ',' }, true);
        if (split.Length == 1 && parseTimeSpan(split[0], typeof(TimeSpan), out var tsValue, null!))
        {
            // time array was expressed as a timespan; create an array from midnight and onward (24h) using this time interval ...
            var interval = (TimeSpan)tsValue!;
            var time = DateTime.MinValue.Add(interval);
            var end = time.AddDays(1);
            while (time < end)
            {
                times.Add(time);
                time = time.Add(interval);
            }

            value = times.ToArray();
            return true;
        }
        
        foreach (var s in split)
        {
            if (!parseDateTime(s, typeof(DateTime), out var obj, null!) || obj is not DateTime dtValue)
            {
                value = useDefault;
                return false;
            }
            times.Add(dtValue);
        }

        value = times.ToArray();
        return true;
    }
        
    static bool parseDateTime(string? stringValue, Type targetType, out object? value, object useDefault)
    {
        var formats = new[] { @"yyyy-MM-dd\THH:mm:ss" };
        if ((targetType != typeof(DateTime) && targetType != typeof(DateTime?)) || stringValue.IsUnassigned())
        {
            value = useDefault;
            return false;
        }

        stringValue = stringValue!.Trim();
        if (!stringValue.Contains("T"))
        {
            // the value is either just a date or just a time ...
            if (stringValue.Contains(":"))
            {
                stringValue = $"{DateTime.Today:yyyy-MM-dd}T{stringValue}";
            }
            else
            {
                var suffix = stringValue.EndsWith("Z", StringComparison.InvariantCultureIgnoreCase)
                    ? "Z"
                    : string.Empty;
                stringValue = $"{stringValue}T00:01:01{suffix}";
            }
                
        }
        var isZuluTime = stringValue.EndsWith("Z", StringComparison.InvariantCultureIgnoreCase);
        if (isZuluTime)
        {
            stringValue = stringValue.Substring(0, stringValue.Length - 1);
        }

        var style = isZuluTime 
            ? DateTimeStyles.AssumeUniversal 
            : DateTimeStyles.AssumeLocal;
        if (DateTime.TryParseExact(stringValue, formats, CultureInfo.InvariantCulture, style, out var dtValue))
        {
            value = dtValue;
            return true;
        }

        value = null;
        return false;
    }

    static bool parseTimeSpan(string? stringValue, Type targetType, out object? value, object useDefault)
    {
        if ((targetType != typeof(TimeSpan) && targetType != typeof(TimeSpan?)) 
            || stringValue.IsUnassigned() 
            || !stringValue!.TryParseTimeSpan(TimeUnits.Seconds, out var timeSpan, ignoreCase:true))
        {
            value = useDefault;
            return false;
        }

        value = timeSpan;
        return true;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;using DCAF.Discord.Scheduling;
using TetraPak.XP;
using TetraPak.XP.StringValues;

namespace DCAF.Discord.Scheduling
{
    /// <summary>
    ///   A filtered and sorted collection of weekdays.
    /// </summary>
    public sealed class WeekdaysCollection : MultiStringValue, IIndefinitelyEnumerable<DayOfWeek>
    {
        static readonly Dictionary<string, DayOfWeek> s_map = new()
        {
            ["su"] = DayOfWeek.Sunday,
            ["sunday"] = DayOfWeek.Sunday, 
            ["mo"] = DayOfWeek.Monday,
            ["monday"] = DayOfWeek.Monday, 
            ["tu"] = DayOfWeek.Tuesday,
            ["tuesday"] = DayOfWeek.Tuesday, 
            ["we"] = DayOfWeek.Wednesday,
            ["wednesday"] = DayOfWeek.Wednesday, 
            ["th"] = DayOfWeek.Thursday,
            ["thursday"] = DayOfWeek.Thursday, 
            ["fr"] = DayOfWeek.Friday,
            ["friday"] = DayOfWeek.Friday, 
            ["sa"] = DayOfWeek.Saturday,
            ["saturday"] = DayOfWeek.Saturday, 
        };

        CircularEnumerator<DayOfWeek>? _enumerator;

        public DayOfWeek[] Weekdays { get; private set; }

        public new static WeekdaysCollection Empty => new();
        
        public new IEnumerator<DayOfWeek> GetEnumerator() => _enumerator ??= new CircularEnumerator<DayOfWeek>(Weekdays);

        protected override Outcome<string[]> OnValidate(string[] items)
        {
            var days = new List<DayOfWeek>();
            for (var i = 0; i < items.Length; i++)
            {
                var key = items[i].ToLowerInvariant();
                if (!s_map.TryGetValue(key, out var weekDay))
                    return Outcome<string[]>.Fail($"Day not recognised: {items[i]}");
                
                if (days.Contains(weekDay))
                    return Outcome<string[]>.Fail($"Day stated multiple times: {items[i]}");
                
                days.Add(weekDay);
            }

            Weekdays = sortAndRemoveDuplicates(days.ToArray());
            return Outcome<string[]>.Success(items);
        }

        static DayOfWeek[] sortAndRemoveDuplicates(DayOfWeek[] daysOfWeek)
        {
            if (!daysOfWeek.Any())
                return Array.Empty<DayOfWeek>();
                
            var list = daysOfWeek.ToList();
            list.Sort((a, b) => a < b ? -1 : a > b ? 1 : 0);
            var sorted = list.ToArray();
            var filtered = new List<DayOfWeek>();
            var last = sorted[0];
            filtered.Add(last);
            for (var i = 1; i < sorted.Length; i++)
            {
                var day = sorted[i];
                if (day == last)
                    continue;

                filtered.Add(day);
                last = day;
            }

            return filtered.ToArray();
        }
        
#pragma warning disable CS8618
        public WeekdaysCollection(string stringValue) 
            : base(stringValue, ",", StringComparison.InvariantCultureIgnoreCase)
        {
            _enumerator ??= new CircularEnumerator<DayOfWeek>(Weekdays!);
        }

        WeekdaysCollection()
        {
            _enumerator = new CircularEnumerator<DayOfWeek>(Array.Empty<DayOfWeek>());
        }
#pragma warning restore CS8618
    }
}

public static class WeekdaysCollectionHelper
{
    /// <summary>
    ///   Gets the next available <see cref="DayOfWeek"/> in the collection after a specified weekday.  
    /// </summary>
    /// <param name="weekdays">
    ///   The available weekdays.
    /// </param>
    /// <param name="current">
    ///   A "current" <see cref="DayOfWeek"/> value. 
    /// </param>
    /// <returns>
    ///   A <see cref="DateTime"/> matching the next available day of week.    
    /// </returns>
    public static DateTime GetNextAfter(this WeekdaysCollection weekdays, DateTime current)
    {
        // var circular = new CircularEnumerator<DayOfWeek>(weekdays.Weekdays); obsolete

        /* nisse
         
           circular:    Su __ __ We __ Fr Sa     
           current:        ^
        */
        
        // find the first day after 'current' ...
        var nextDayOfWeek = weekdays.Weekdays[0];
        if (weekdays.Count == 1)
            return current.AddDays(7);

        var i = 0;
        for (; nextDayOfWeek < current.DayOfWeek && i < weekdays.Count; i++)
        {
            nextDayOfWeek = weekdays.Weekdays[i]; 
        }

        if (nextDayOfWeek == current.DayOfWeek)
        {
            nextDayOfWeek = i == weekdays.Count
                ? weekdays.Weekdays[0]
                : weekdays.Weekdays[i];
        }
            
        // find the date corresponding to the next available day ...
        do
        {
            current = current.AddDays(1);
            
        } while (nextDayOfWeek != current.DayOfWeek);

        return current;
    }
}

public interface IIndefinitelyEnumerable<out T> : IEnumerable<T>
{}

public sealed class CircularEnumerator<T> : IEnumerator<T>
{
    readonly IEnumerator<T> _enumerator;

    public bool MoveNext()
    {
        if (_enumerator.MoveNext()) 
            return true;
        
        _enumerator.Reset();
        return _enumerator.MoveNext();
    }

    public void Reset() => _enumerator.Reset();

    public T Current => _enumerator.Current;

    object IEnumerator.Current => ((IEnumerator)_enumerator).Current!;

    public void Dispose() => _enumerator.Dispose();

    public CircularEnumerator(IEnumerable<T> items)
    {
        _enumerator = items.GetEnumerator();
        _enumerator.MoveNext();
    }
}
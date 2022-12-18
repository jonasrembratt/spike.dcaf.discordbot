using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetraPak.XP;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Scheduling;

public sealed class InfiniteDateTimes : IEnumerable<DateTime>
{
    readonly WeekdaysCollection _weekdays;
    readonly DateTime[] _times;
    readonly InfiniteEnumerator _enumerator;

    public IEnumerator<DateTime> GetEnumerator() => _enumerator; 

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    sealed class InfiniteEnumerator : IEnumerator<DateTime>
    {
        readonly InfiniteDateTimes _dateTimes;
        DateTime _current;
        // int _idxTime;
        readonly DateTime _start;
        readonly ILog? _log;
        // readonly LogBuffer _temp_infiniteEnumeratorIssueLog; // todo Remove after resolving the issue
        // int _temp_expectedHoursIdx = -1;
        // readonly int[] _temp_expectedHours;

        DateTime getNext2(DateTime from)
        {
            // using var logSection = _log?.Section(
            //     $"--- IndefiniteEnumerator.getNext2({from.ToStandardString()})", 
            //     sectionSuffix:"---");

            var currentDayIdx = _dateTimes._weekdays.Weekdays.IndexOf(dow => dow == from.DayOfWeek);
            if (currentDayIdx == -1)
                return getFirstTimeNextDay(from);

            var fromTime = from.GetTime();
            var nextTimeIdx = _dateTimes._times.IndexOf(dt => dt > fromTime);
            if (nextTimeIdx == -1)
                return getFirstTimeNextDay(from);

            var time = _dateTimes._times[nextTimeIdx];
            return from.SetTime(time);
        }

        DateTime getFirstTimeNextDay(DateTime from)
        {
            var nextDowIdx = _dateTimes._weekdays.Weekdays.IndexOf(dow => dow > from.DayOfWeek);
            var nextDayOfWeek = nextDowIdx == -1
                ? _dateTimes._weekdays.Weekdays[0]
                : _dateTimes._weekdays.Weekdays[nextDowIdx];

            var day = from;
            while (day.DayOfWeek != nextDayOfWeek)
            {
                day = day.AddDays(1);
            }

            var next = day.SetTime(_dateTimes._times[0]);
            // handle edge case where configuration specifies just one day and one time (once a week) ...
            return from == next 
                ? next.AddDays(7) 
                : next;
        }

        // DateTime getNext(DateTime from)
        // {
        //     using var logSection = _log?.Section(
        //         $"--- IndefiniteEnumerator.getNext({from.ToStandardString()})", 
        //         sectionSuffix:"---");
        //     logSection.Debug($"from={from.ToStandardString()}");
        //     
        //     var day = _dateTimes._weekdays.Weekdays.FirstOrDefault(d => d >= from.DayOfWeek);
        //     
        //     logSection.Debug($"day={day}");
        //     
        //     var next = from;
        //     while (next.DayOfWeek != day)
        //     {
        //         next = next.AddDays(1);
        //     }
        //     
        //     logSection.Debug($"next={next}");
        //
        //     if (_dateTimes._times.Length == 0)
        //         return next;
        //
        //     try
        //     {
        //         logSection.Debug($"_dateTimes._times=[{_dateTimes._times.ConcatEnumerable()}]");
        //         logSection.Debug($"_idxTime={_idxTime}");
        //
        //         var time = _dateTimes._times[_idxTime];
        //
        //         logSection.Debug($"time={time.ToStandardString()}");
        //
        //         // _temp_infiniteEnumeratorIssueLog.Trace($"[46] next.Year={next.Year}, next.Month={next.Month}, next.Day={next.Day}, time.Hour={time.Hour}, time.Minute={time.Minute}, time.Second={time.Second}");
        //         next = new DateTime(next.Year, next.Month, next.Day, time.Hour, time.Minute, time.Second);
        //       
        //         logSection.Debug($"next={next.ToStandardString()}");
        //         
        //         var prev = next;
        //         while (next <= from)
        //         {
        //             if (++_idxTime != _dateTimes._times.Length)
        //             {
        //                 time = _dateTimes._times[_idxTime];
        //       
        //                 logSection.Debug($"_idxTime={_idxTime}");
        //                 logSection.Debug($"time={time.ToStandardString()}");
        //         
        //                 // _temp_infiniteEnumeratorIssueLog.Trace($"[54] next.Year={next.Year}, next.Month={next.Month}, next.Day={next.Day}, time.Hour={time.Hour}, time.Minute={time.Minute}, time.Second={time.Second}");
        //                 next = new DateTime(next.Year, next.Month, next.Day, time.Hour, time.Minute, time.Second);
        //                 
        //                 logSection.Debug($"next={next}");
        //
        //                 if (next < prev)
        //                 {
        //                     logSection.Debug($"condition true: next < prev ({prev.ToStandardString()})");
        //
        //                     // _temp_infiniteEnumeratorIssueLog.Trace($"[58] next.Year={next.Year}, next.Month={next.Month}, next.Day={next.Day}, time.Hour={time.Hour}, time.Minute={time.Minute}, time.Second={time.Second}");
        //                     next = next.AddDays(1);
        //                 
        //                     logSection.Debug($"next={next.ToStandardString()}");
        //                 }
        //
        //                 prev = next;
        //                 continue;
        //             }
        //             _idxTime = 0;
        //             time = _dateTimes._times[0];
        //             var after = _dateTimes._weekdays.GetNextAfter(next);
        //             // _temp_infiniteEnumeratorIssueLog.Trace($"[68] after.Year={after.Year}, after.Month={after.Month}, after.Day={after.Day}, time.Hour={time.Hour}, time.Minute={time.Minute}, time.Second={time.Second}");
        //             next = new DateTime(after.Year, after.Month, after.Day, time.Hour, time.Minute, time.Second);
        //             prev = next;
        //             
        //             logSection.Debug($">> _idxTime={_idxTime}, time={time.ToStandardString()}, after={after.ToStandardString()}");
        //             logSection.Debug($">> time={time.ToStandardString()}");
        //             logSection.Debug($">> after={after.ToStandardString()}");
        //             logSection.Debug($">> next={next.ToStandardString()}");
        //             logSection.Debug($">> prev={prev.ToStandardString()}");
        //         }
        //
        //         // ++_temp_expectedHoursIdx;
        //         // if (_temp_expectedHoursIdx == _temp_expectedHours.Length)
        //         // {
        //         //     _temp_expectedHoursIdx = 0;
        //         // }
        //         // if (next.Hour != _temp_expectedHours[_temp_expectedHoursIdx])
        //         // {
        //         //     XpDateTime.TryStopTime();
        //         //     _log.Warning($"Unexpected 'next' time: {next.ToStandardString()}");
        //         //     XpDateTime.TryResumeTime();
        //         // }
        //
        //         return next;
        //     }
        //     catch (Exception)
        //     {
        //         XpDateTime.TryStopTime();
        //         // _temp_infiniteEnumeratorIssueLog.WriteToLog(_log!);
        //         throw;
        //     }
        // }

        public bool MoveNext()
        {
            _current = getNext2(_current);
            return true;
        }

        public void Reset()
        {
            _current = _start;
            // _idxTime = 0; obsolete
        }

        public DateTime Current
        {
            get
            {
                if (_current == _start)
                {
                    MoveNext();
                }

                return _current;
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        { }

        internal InfiniteEnumerator(InfiniteDateTimes dateTimes, DateTime start/*, DateTime[] temp_times*/, ILog? log = null)
        {
            // _temp_infiniteEnumeratorIssueLog = new LogBuffer("IndefiniteEnumerator.getNext - issue", 100);
            // _temp_expectedHours = temp_times.Select(dt => dt.Hour).ToArray();
            _log = log;
            _dateTimes = dateTimes;
            _start = start;
            _current = start; 
        }
    }

    static DateTime[] extractAndSort(DateTime[] times)
    {
        var list = times.Select(dt => dt.GetTime()).ToList();
        list.Sort((a,b) => a < b ? -1 : a > b ? 1 : 0);
        return list.ToArray();
    }

    public InfiniteDateTimes(WeekdaysCollection weekdays, DateTime[] times, DateTime? start = null, ILog? log = null)
    {
        _weekdays = weekdays;
        if (start is null)
        {
            var today = XpDateTime.Today;
            var time = times[0];
            start = new DateTime(today.Year, today.Month, today.Day, time.Hour, time.Minute, time.Second, time.Millisecond)
                .Subtract(TimeSpan.FromMinutes(1));
        }
        _times = extractAndSort(times);
        _enumerator = new InfiniteEnumerator(this, start.Value/*, times*/, log);
    }
}
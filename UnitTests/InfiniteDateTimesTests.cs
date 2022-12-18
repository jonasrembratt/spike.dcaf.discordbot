using System;
using DCAF.Discord.Scheduling;
using Xunit;

namespace UnitTests;

public sealed class IndefiniteDateTimesTests
{
    [Fact]
    public void Ensure_is_infinite()
    {
        var weekDays = new WeekdaysCollection("Mo,We,Su,Fr"); // <-- deliberately unsorted
        var z = DateTime.MinValue;
        var times = new DateTime[]
        {
            new(z.Year, z.Month, z.Day, 5,0, 0),
            new(z.Year, z.Month, z.Day,14,0, 0),
        };
        var dateTimes = new InfiniteDateTimes(weekDays, times, null!);
        var count = 1000;
        var previous = DateTime.MinValue;
        foreach (var time in dateTimes)
        {
            Assert.NotEqual(previous, time);
            Assert.Contains(time.DayOfWeek, weekDays.Weekdays);
            if (--count == 0)
                break;
            
            previous = time;
        }
        Assert.Equal(0, count);
    }

    [Fact]
    public void Test_edge_case_with_just_one_weekday()
    {
        var weekdays = new WeekdaysCollection("Su");
        var z = DateTime.MinValue;
        var times = new DateTime[]
        {
            new(z.Year, z.Month, z.Day, 3,0, 0, DateTimeKind.Utc),
            new(z.Year, z.Month, z.Day,9,0, 0, DateTimeKind.Utc),
        };
        var dateTimes = new InfiniteDateTimes(weekdays, times, null!);
        var count = 100;
        var previous = DateTime.MinValue;
        foreach (var time in dateTimes)
        {
            Assert.NotEqual(previous, time);
            Assert.Contains(time.DayOfWeek, weekdays.Weekdays);
            if (--count == 0)
                break;
            
            previous = time;
        }
        Assert.Equal(0, count);
    }
    
    [Fact]
    public void Test_edge_case_with_two_weekdays_and_one_time()
    {
        var weekdays = new WeekdaysCollection("Su, We");
        var z = DateTime.MinValue;
        var times = new DateTime[]
        {
            new(z.Year, z.Month, z.Day, 3,0, 0, DateTimeKind.Utc),
        };
        var dateTimes = new InfiniteDateTimes(weekdays, times, null!);
        var count = 100;
        var previous = DateTime.MinValue;
        foreach (var time in dateTimes)
        {
            Assert.NotEqual(previous, time);
            Assert.Contains(time.DayOfWeek, weekdays.Weekdays);
            if (--count == 0)
                break;
            
            previous = time;
        }
        Assert.Equal(0, count);
    }

    [Fact]
    public void Test_edge_case_with_just_one_weekday_and_one_time()
    {
        var weekdays = new WeekdaysCollection("Su");
        var z = DateTime.MinValue;
        var times = new DateTime[]
        {
            new(z.Year, z.Month, z.Day, 3,0, 0, DateTimeKind.Utc),
        };
        var dateTimes = new InfiniteDateTimes(weekdays, times, null!);
        var count = 100;
        var previous = DateTime.MinValue;
        foreach (var time in dateTimes)
        {
            Assert.NotEqual(previous, time);
            Assert.Contains(time.DayOfWeek, weekdays.Weekdays);
            if (--count == 0)
                break;
            
            previous = time;
        }
        Assert.Equal(0, count);
    }

    [Fact]
    public void Ensure_all_times_are_included()
    {
        var weekdays = new WeekdaysCollection("Su");
        var z = DateTime.MinValue;
        var times = new DateTime[]
        {
            new(z.Year, z.Month, z.Day, 5,0, 0, DateTimeKind.Utc),
            new(z.Year, z.Month, z.Day, 11,0, 0, DateTimeKind.Utc),
            new(z.Year, z.Month, z.Day, 17,0, 0, DateTimeKind.Utc),
            new(z.Year, z.Month, z.Day, 23,0, 0, DateTimeKind.Utc)
        };
        var dateTimes = new InfiniteDateTimes(weekdays, times);
        var i = -1;
        foreach (var time in dateTimes)
        {
            Assert.Equal(times[++i].Hour, time.Hour);
            if (i == 3)
                break;
        }
        Assert.Equal(3, i);
    }
    
    [Fact]
    public void Ensure_no_day_is_skipped()
    {
        var weekdays = new WeekdaysCollection("Mo,Tu,We,Th,Fr,Sa,Su");
        var z = DateTime.MinValue;
        var times = new DateTime[]
        {
            new(z.Year, z.Month, z.Day, 5,0, 0, DateTimeKind.Utc),
            new(z.Year, z.Month, z.Day, 11,0, 0, DateTimeKind.Utc),
            new(z.Year, z.Month, z.Day, 17,0, 0, DateTimeKind.Utc),
            new(z.Year, z.Month, z.Day, 23,0, 0, DateTimeKind.Utc)
        };
        var dateTimes = new InfiniteDateTimes(weekdays, times, null!);
        
        // run three weeks ...
        var testRuns = 10; //weekdays.Count * times.Length * 3;

        DateTime? prev = null;
        var i = -1;
        foreach (var next in dateTimes)
        {
            ++i;
            if (prev is null)
            {
                prev = next;
                continue;
            }

            var diff = next.Subtract(prev.Value);
            Assert.True(diff.Days < 1);
            prev = next;
            if (i == testRuns)
                break;
        }
    }
}
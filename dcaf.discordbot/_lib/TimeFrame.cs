using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DCAF.DiscordBot._lib
{
    [DebuggerDisplay("{ToString()}")]
    public class TimeFrame : IComparable<TimeFrame>
    {
        public DateTime From { get; }
        
        public DateTime To { get; }

        protected bool Equals(TimeFrame other) => From.Equals(other.From) && To.Equals(other.To);

        public int CompareTo(TimeFrame? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            return ReferenceEquals(null, other) ? 1 : From.CompareTo(other.From);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TimeFrame)obj);
        }

        public override int GetHashCode() => HashCode.Combine(From, To);

        public static bool operator ==(TimeFrame? left, TimeFrame? right) => Equals(left, right);

        public static bool operator !=(TimeFrame? left, TimeFrame? right) => !Equals(left, right);

        public override string ToString() => $"{From:s} -- {To:s}";

        public TimeFrame(DateTime from, DateTime to)
        {
            From = from;
            To = to;
        }
    }

    public static class TimeFrameHelper
    {
        public static Overlap GetOverlap(this TimeFrame self, TimeFrame other)
        {
            if (self.To <= other.From || self.From >= other.To)
                return Overlap.None;

            if (self.From <= other.From && self.To >= other.To)
                return Overlap.Full;

            if (other.From <= self.From && other.To >= self.To)
                return Overlap.Full;

            if (self.From < other.From)
                return Overlap.Start;

            return Overlap.End;
        }

        public static TimeFrame Merge(this TimeFrame self, TimeFrame other)
        {
            switch (self.GetOverlap(other))
            {
                case Overlap.None:
                    return self;
                
                case Overlap.Full:
                    return self.From <= other.From
                        ? self
                        : other;
                
                case Overlap.Start:
                case Overlap.End:
                    return new TimeFrame(
                        new DateTime(Math.Min(self.From.Ticks, other.From.Ticks)), 
                        new DateTime(Math.Max(self.To.Ticks, other.To.Ticks)));
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///   Subtracts one or more chronologically ordered time frames from this time frame.
        /// </summary>
        /// <param name="self">
        ///   The time frame to be subtracted from.
        /// </param>
        /// <param name="timeFrames">
        ///   The timeframes to be subtracted.
        /// </param>
        /// <returns>
        ///   The difference; zero or more remaining timeframe(s) after <paramref name="timeFrames"/>
        ///   was subtracted from the time frame.
        /// </returns>
        /// <remarks>
        ///   NOTE: The <paramref name="timeFrames"/> are assumed to be chronologically sorted.
        ///   If not then please use <see cref="Subtract(TimeFrame,bool,TimeFrame[])"/> instead.
        /// </remarks>
        public static TimeFrame[] Subtract(this TimeFrame self, params TimeFrame[] timeFrames) 
            => 
            self.Subtract(true, timeFrames);

        /// <summary>
        ///   Subtracts one or more time frames from the time frame while specifying whether
        ///   the timeframe collection is chronologically sorted.
        /// </summary>
        /// <param name="self">
        ///   The time frame to be subtracted from.
        /// </param>
        /// <param name="isSorted">
        ///   Specifies whether <paramref name="timeFrames"/> is chronologically sorted.
        /// </param>
        /// <param name="timeFrames">
        ///   The timeframes to be subtracted.
        /// </param>
        /// <returns>
        ///   The difference; zero or more remaining timeframe(s) after <paramref name="timeFrames"/>
        ///   was subtracted from the time frame.
        /// </returns>
        public static TimeFrame[] Subtract(this TimeFrame self, bool isSorted, params TimeFrame[] timeFrames)
        {
            var list = new List<TimeFrame>();
            if (!isSorted)
            {
                var sorted = timeFrames.ToList();
                sorted.Sort((a, b) => a.CompareTo(b));
                timeFrames = sorted.ToArray();
            }
            for (var i = 0; i < timeFrames.Length; i++)
            {
                var nextTimeFrame = timeFrames[i];
                var diff = self.Subtract(nextTimeFrame);
                switch (diff.Length)
                {
                    case 0:
                        if (self.To < nextTimeFrame.From)
                            return Array.Empty<TimeFrame>();
                        break;
                    
                    case 1:
                        list.AddRange(diff);
                        if (self.To < nextTimeFrame.From)
                            return list.ToArray();
                        break;

                    case 2:
                        if (i == timeFrames.Length - 1)
                        {
                            list.AddRange(diff);
                            return list.ToArray();
                        }
                        list.Add(diff[0]);
                        self = diff[^1];
                        break;
                }
            }
            throw new NotSupportedException($"Unexpected difference when subtracting from time frame {self}");
        }

        public static TimeFrame[] Subtract(this TimeFrame self, TimeFrame other)
        {
            switch (self.GetOverlap(other))
            {
                case Overlap.None:
                    return new[] { self };
                
                case Overlap.Full:
                    var intersected = new List<TimeFrame>();
                    if (self.From < other.From)
                    {
                        intersected.Add(new TimeFrame(self.From, other.From));
                    }
                    if (self.To > other.To)
                    {
                        intersected.Add(new TimeFrame(other.To, self.To));
                    }
                    return intersected.ToArray();

                case Overlap.Start:
                    return self.From < other.From 
                        ? new[] { new TimeFrame(self.From, other.From) } 
                        : Array.Empty<TimeFrame>();

                case Overlap.End:
                    return self.To > other.To
                        ? new[] { new TimeFrame(other.To, self.To) } 
                        : Array.Empty<TimeFrame>();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum Overlap
    {
        None,
        Full,
        Start,
        End
    }
}
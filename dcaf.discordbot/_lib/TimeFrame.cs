using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace DCAF.DiscordBot._lib
{
    [DebuggerDisplay("{ToString()}")]
    public class TimeFrame
    {
        public DateTime From { get; }
        
        public DateTime To { get; }

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
            if (self.From > other.To || self.To < other.From)
                return Overlap.None;

            if (self.From <= other.To && self.To >= other.To)
                return Overlap.Full;

            if (self.From <= other.To)
                return Overlap.Start;

            return Overlap.End;
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
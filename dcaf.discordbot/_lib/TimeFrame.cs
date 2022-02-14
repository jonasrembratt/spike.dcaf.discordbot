using System;

namespace DCAF.DiscordBot._lib
{
    public class TimeFrame
    {
        public DateTime From { get; }
        
        public DateTime To { get; }

        public TimeFrame(DateTime from, DateTime to)
        {
            From = from;
            To = to;
        }
    }
}
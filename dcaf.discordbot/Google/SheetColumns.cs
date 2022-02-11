using System;

namespace DCAF.DiscordBot.Google
{
    public class SheetColumns
    {
        public string First { get;  }
        public string? Last { get; }

        public SheetColumns(string first, string? last)
        {
            if (string.IsNullOrWhiteSpace(first)) throw new ArgumentNullException(nameof(first));
            var useLast = last ?? first;
            
            if (string.CompareOrdinal(first, useLast) > 0)
            {
                (first, useLast) = (useLast, first);
            }
            First = first;
            Last = useLast;
        }
    }
}
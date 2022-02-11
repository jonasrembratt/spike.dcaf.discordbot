using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace DCAF.DiscordBot.Model
{
    [DebuggerDisplay("{ToString()}")]
    public class Event : IEnumerable<EventEntry>
    {
        List<EventEntry> _entries;

        public int Count => _entries.Count;
        
        public string Name { get; set; }
        
        public DateTime DateTime { get; set; }

        public override string ToString() => $"{Name} {DateTime:u} ({Count})";

        public IEnumerator<EventEntry> GetEnumerator() => _entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_entries).GetEnumerator();

        public Event(string name, DateTime dateTime, List<EventEntry> entries)
        {
            Name = name;
            DateTime = dateTime;
            _entries = entries;
        }
    }
}
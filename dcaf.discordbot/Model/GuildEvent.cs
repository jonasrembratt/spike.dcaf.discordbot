using System;
using System.Diagnostics;
using System.Linq;

namespace DCAF.DiscordBot.Model
{
    [DebuggerDisplay("{ToString()}")]
    public class GuildEvent
    {
        static readonly string[] s_dateTimeFormats =  {  "dd-MM-yyyy", "d-M-yyyy"  };

        public string Title { get; }
        
        public DateTime Date { get; }

        public GuildEventSignup[] Signups { get; }

        public ulong EventId { get; set; }

        public override string ToString() => $"{Title} {Date:s} ({Signups.Length} signups)";

        public GuildEvent(RaidHelperEvent rhEvent)
        {
            Title = rhEvent.Title;
            try
            {
                Date = DateTime.ParseExact(rhEvent.Date, s_dateTimeFormats, null);
                Signups = rhEvent.Signups.Select(s => new GuildEventSignup(s)).ToArray();
                EventId = ulong.Parse(rhEvent.RaidId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e); // nisse
                throw;
            }
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public class GuildEventSignup
    {
        static readonly string[] s_dateTimeFormats = { "yyyy-MM-ddTHH:mm:ss.ffffff" };
        
        public string Role { get; }

        public PersonnelName Name { get; }

        public ulong DiscordId { get; }

        public DateTime Timestamp { get; }

        public override string ToString() => $"{Name} @{Timestamp:s}";

        public GuildEventSignup(RaidHelperEventSignup signup)
        {
            Role = (string) signup["role"]!;
            Name = new PersonnelName((string)signup["name"]!);
            DiscordId = ulong.Parse((string)signup["userid"]!);
            Timestamp = DateTime.ParseExact((string)signup["timestamp"]!, s_dateTimeFormats, null);
        }
    }
    
}
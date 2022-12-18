using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Model
{
    [DebuggerDisplay("{ToString()}")]
    public sealed class GuildEvent
    {
        static readonly string[] s_dateTimeFormats =  
        { 
            "dd-MM-yyyy", 
            "d-M-yyyy", 
            "yyyy-MM-ddTHH:mm:ss.fff", 
            "yyyy-MM-ddTHH:mm:ss.ffffff" 
        };

        public string Title { get; }
        
        public DateTime Date { get; }

        public GuildEventSignup[] Signups { get; }

        public ulong EventId { get; set; }

        public override string ToString() => $"{Title} {Date:s} ({Signups.Length} signups)";

        internal static bool TryParseDate(string stringValue, out DateTime result)
        {
            
            return DateTime.TryParseExact(stringValue, s_dateTimeFormats, null, DateTimeStyles.None, out result);

// #if NET5_0_OR_GREATER                
//                 Date = DateTime.ParseExact(rhEvent.Date, s_dateTimeFormats, null);
// #else
//             var noParse = true;
//             for (var i = 0; i < s_dateTimeFormats.Length && noParse; i++)
//             {
//                 var format = s_dateTimeFormats[i];
//                 try
//                 {
//                     DateTime.TryParseExact(stringValue, s_dateTimeFormats, null, null, out var result);
//                     noParse = false;
//                 }
//                 catch (Exception ex)
//                 {
//                     // 
//                 }
//             }
//             if (noParse)
//                 throw new FormatException($"Invalid guild event date/time format: \"{rhEvent.Date}\"");
// #endif
        }

        public GuildEvent(RaidHelperEvent rhEvent, ILog? log)
        {
            Title = rhEvent.Title;
            try
            {
                if (!TryParseDate(rhEvent.Date, out var eventDate))
                    throw new FormatException($"Invalid guild event date/time format: \"{rhEvent.Date}\"");

                Date = eventDate;
                Signups = rhEvent.Signups.Select(s => new GuildEventSignup(s)).ToArray();
                EventId = ulong.Parse(rhEvent.RaidId);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public sealed class GuildEventSignup
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
            if (!GuildEvent.TryParseDate((string)signup["timestamp"]!, out var timestamp))
                throw new FormatException($"Invalid guild event date/time format: \"{(string)signup["timestamp"]!}\"");

            Timestamp = timestamp;
        }
    }
    
}
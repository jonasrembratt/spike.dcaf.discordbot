using System;
using System.Diagnostics;
using TetraPak.XP;

namespace DCAF.DiscordBot.Model
{
    [DebuggerDisplay("{ToString()}")]
    public class PersonnelName : IStringValue
    {
        const char CallsignQualifier1 = '\''; 
        const char CallsignQualifier2 = '\"'; 
        
        public string StringValue { get; }

        public string Forename { get; }

        public string Surname { get; }

        public string? Callsign { get; }

        public override string ToString()
        {
            if (Callsign.IsAssigned())
                return $"{Forename} '{Callsign}' {Surname}";
            
            var forename = Forename.IsAssigned() ? $"{Forename[0]}." : string.Empty;
            return $"{forename} {Surname}";
        }

        static bool tryParse(string stringValue, out string forename, out string surname, out string callsign)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                forename = surname = callsign = string.Empty;
                return false;
            }

            var split = stringValue.Split(
                new[] { CallsignQualifier1, CallsignQualifier2 },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            switch (split.Length)
            {
                case 1:
                    forename = callsign = string.Empty;
                    surname = split[0];
                    return true;
                
                case 2:
                    forename = split[0]; 
                    surname = split[1];
                    callsign = string.Empty;
                    return true;

                default:
                    forename = split[0]; 
                    surname = split[1];
                    callsign = split[2];
                    return true;
            }
        }

        public PersonnelName(string stringValue)
        {
            if (!tryParse(stringValue, out var forename, out var surname, out var callsign))
                throw new FormatException($"Invalid personnel name: {stringValue}");

            Forename = forename;
            Surname = surname;
            Callsign = callsign;
        }
    }
}
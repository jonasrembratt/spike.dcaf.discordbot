using System.Diagnostics;
using DCAF._lib;
using TetraPak.XP;
using TetraPak.XP.StringValues;

namespace DCAF.Model
{
    [DebuggerDisplay("{ToString()}")]
    public sealed class PersonnelName : MultiStringValue //  IStringValue obsolete
    {
        const char CallsignQualifier1 = '\''; 
        const char CallsignQualifier2 = '\"'; 
        
        // public string StringValue { get; } obsolete

        public string? Forename { get; private set; }

        public string? Surname { get; private set; }

        public string? Callsign { get; private set; }

        public override string ToString()
        {
            if (Callsign.IsAssigned())
                return $"{Forename} '{Callsign}' {Surname}";
            
            var forename = Forename.IsAssigned() ? $"{Forename![0].ToString()}." : string.Empty;
            return $"{forename} {Surname}";
        }

        protected override StringValueParseResult OnParse(string? stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                Forename = Surname = Callsign = string.Empty;
                return StringValueParseResult.Empty;
            }

            var split = stringValue.SplitAndTrim(new[] { CallsignQualifier1, CallsignQualifier2 }, true);
            switch (split.Length)
            {
                case 1:
                    Forename = Callsign = string.Empty;
                    Surname = split[0];
                    return new StringValueParseResult(Surname, Surname.GetHashCode());
                
                case 2:
                    Forename = split[0]; 
                    Surname = split[1];
                    Callsign = string.Empty;
                    var useStringValue = $"{Forename} {Surname}";
                    return new StringValueParseResult(useStringValue, useStringValue.GetHashCode());

                default:
                    Forename = split[0]; 
                    Surname = split[1];
                    Callsign = split[2];
                    useStringValue = $"{Forename} {CallsignQualifier1}{Callsign}{CallsignQualifier1} {Surname}";
                    return new StringValueParseResult(useStringValue, useStringValue.GetHashCode());
            }
        }

        // static bool tryParse(string stringValue, out string forename, out string surname, out string callsign) obsolete
        // {
        //     if (string.IsNullOrWhiteSpace(stringValue))
        //     {
        //         forename = surname = callsign = string.Empty;
        //         return false;
        //     }
        //
        //     var split = stringValue.Split(
        //         new[] { CallsignQualifier1, CallsignQualifier2 },
        //         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        //
        //     switch (split.Length)
        //     {
        //         case 1:
        //             forename = callsign = string.Empty;
        //             surname = split[0];
        //             return true;
        //         
        //         case 2:
        //             forename = split[0]; 
        //             surname = split[1];
        //             callsign = string.Empty;
        //             return true;
        //
        //         default:
        //             forename = split[0]; 
        //             surname = split[1];
        //             callsign = split[2];
        //             return true;
        //     }
        // }

        public PersonnelName(string stringValue) : base(stringValue)
        {
            // if (!tryParse(stringValue, out var forename, out var surname, out var callsign)) obsolete
            //     throw new FormatException($"Invalid personnel name: {stringValue}");
            //
            // Forename = forename;
            // Surname = surname;
            // Callsign = callsign;
        }
    }
}
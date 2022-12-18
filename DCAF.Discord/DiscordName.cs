using System;
using System.Diagnostics;
using DCAF._lib;
using TetraPak.XP.StringValues;

#pragma warning disable CS0659
#pragma warning disable CS8618
#pragma warning disable CS0660, CS0661

namespace DCAF.Discord
{
    [DebuggerDisplay("{ToString()}")]
    public sealed class DiscordName : MultiStringValue
    {
        public string? Discriminator { get; private set; }

        public string Name { get; private set; }

        bool Equals(DiscordName other) 
            =>
            string.Equals(Discriminator, other.Discriminator, StringComparison.InvariantCultureIgnoreCase) 
            && string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((DiscordName)obj);
        }

        public static bool operator ==(DiscordName? left, DiscordName? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DiscordName? left, DiscordName? right)
        {
            return !Equals(left, right);
        }

        protected override StringValueParseResult OnParse(string? stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
                throw new ArgumentException("Discord name cannot be null/empty", nameof(stringValue));
                
            var split = stringValue!.SplitAndTrim(new[] {'#'}, true);
            var hashCode = 0;
            switch (split.Length)
            {
                case 0:
                    throw new FormatException($"Invalid Discord name: {stringValue}");
                
                case 1:
                    stringValue = Name = stringValue!.Trim();
                    break;
                
                default:
                    Name = split[0].Trim();
                    Discriminator = split[1].Trim();
                    stringValue = $"{Name}#{Discriminator}";
                    break;
            }

            return new StringValueParseResult(stringValue, stringValue.GetHashCode());
        }

        public DiscordName(string name) 
        : base(name, "#")
        {
        }
        
        public DiscordName(string name, string discriminator) 
        : base($"{name}#{discriminator}")
        {
        }
    }
}
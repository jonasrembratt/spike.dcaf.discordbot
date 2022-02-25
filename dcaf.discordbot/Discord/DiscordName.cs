using System;
using System.Diagnostics;
using TetraPak.XP;

namespace dcaf.discordbot.Discord
{
    [DebuggerDisplay("{ToString()}")]
    public class DiscordName : MultiStringValue
    {
        // readonly string _stringValue; obsolete
        // readonly int _hashCode;

        public string? Discriminator { get; private set; }

        public string Name { get; private set; }

        // public override string ToString() => _stringValue; obsolete

        protected bool Equals(DiscordName other) 
            =>
            string.Equals(Discriminator, other.Discriminator, StringComparison.InvariantCultureIgnoreCase) 
            && string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((DiscordName)obj);
        }

        // public override int GetHashCode() => _hashCode; obsolete

        public static bool operator ==(DiscordName? left, DiscordName? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DiscordName? left, DiscordName? right)
        {
            return !Equals(left, right);
        }

        int getHashCode()
        {
            var code = base.GetHashCode();
            if (code != 0)
                return code;
                
            var hashCode = new HashCode();
            hashCode.Add(Name, StringComparer.InvariantCultureIgnoreCase);
            if (Discriminator is { })
            {
                hashCode.Add(Discriminator, StringComparer.InvariantCultureIgnoreCase);
            }
            return hashCode.ToHashCode();
        }

        protected override StringValueParseResult OnParse(string? stringValue)
        {
            if (string.IsNullOrWhiteSpace(stringValue))
                throw new ArgumentException("Discord name cannot be null/empty", nameof(stringValue));
                
            var split = stringValue.Split('#', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            switch (split.Length)
            {
                case 0:
                    throw new FormatException($"Invalid Discord name: {stringValue}");
                
                case 1:
                    Name = stringValue.Trim();
                    break;
                
                default:
                    Name = split[0].Trim();
                    Discriminator = split[1].Trim();
                    break;
            }

            return new StringValueParseResult(stringValue, getHashCode());
        }

        public DiscordName(string name) : base(name, "#")
        {
            // _stringValue = name; obsolete
            // var split = name.Split('#', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            // switch (split.Length)
            // {
            //     case 0:
            //         throw new FormatException($"Invalid Discord name: {name}");
            //     
            //     case 1:
            //         Name = name.Trim();
            //         break;
            //     
            //     default:
            //         Name = split[0].Trim();
            //         Discriminator = split[1].Trim();
            //         break;
            // }
            // _hashCode = getHashCode();
        }
        
        public DiscordName(string name, string discriminator) : base($"{name}#{discriminator}")
        {
            // _stringValue = $"{name} #{discriminator}"; obsolete
            // Name = name;
            // Discriminator = discriminator;
            // _hashCode = getHashCode();
        }
    }
}
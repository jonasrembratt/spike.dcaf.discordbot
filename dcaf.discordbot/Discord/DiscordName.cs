using System;
using System.Diagnostics;

namespace dcaf.discordbot.Discord
{
    [DebuggerDisplay("{ToString()}")]
    public class DiscordName
    {
        readonly string _stringValue;
        readonly int _hashCode;

        public string? Discriminator { get; }

        public string Name { get; }

        public override string ToString() => _stringValue;

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

        public override int GetHashCode() => _hashCode;

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
            if (_hashCode != 0)
                return _hashCode;
                
            var hashCode = new HashCode();
            hashCode.Add(Name, StringComparer.InvariantCultureIgnoreCase);
            if (Discriminator is { })
            {
                hashCode.Add(Discriminator, StringComparer.InvariantCultureIgnoreCase);
            }
            return hashCode.ToHashCode();
        }

        public DiscordName(string name)
        {
            _stringValue = name;
            var split = name.Split('#', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            switch (split.Length)
            {
                case 0:
                    throw new FormatException($"Invalid Discord name: {name}");
                
                case 1:
                    Name = name.Trim();
                    break;
                
                default:
                    Name = split[0].Trim();
                    Discriminator = split[1].Trim();
                    break;
            }
            _hashCode = getHashCode();
        }
        
        public DiscordName(string name, string discriminator)
        {
            _stringValue = $"{name} #{discriminator}";
            Name = name;
            Discriminator = discriminator;
            _hashCode = getHashCode();
        }
    }
}
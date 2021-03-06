using System;
using System.Text.Json.Serialization;
using DCAF.DiscordBot.Model;
using TetraPak.XP.DynamicEntities;
using TetraPak.XP.Serialization;

namespace DCAF.Model
{
    [JsonConverter(typeof(DynamicEntityJsonConverter<Member>))]
    [JsonKeyFormat(KeyTransformationFormat.None)]
    public class Member : ModifiableEntity
    {
        public const string MissingId = "?";
        
        public string Id
        {
            get => Get<string>()!; 
            set => Set(value);
        }
        
        public bool IsIdentifiable => Id != MissingId;

        public string Forename
        {
            get => Get<string>()!; 
            set => Set(value, true);
        }

        public string? Surname
        {
            get => Get<string>()!; 
            set => Set(value, true);
        }

        public string? Callsign
        {
            get => Get<string?>(); 
            set => Set(value, true);
        }
        
        public string? Email
        {
            get => Get<string?>(); 
            set => Set(value, true);
        }

        public override string ToString()
        {
            var callsign = string.IsNullOrWhiteSpace(Callsign) ? string.Empty : $"'{Callsign}' ";
            var forename = string.IsNullOrWhiteSpace(callsign)
                ? string.IsNullOrWhiteSpace(Forename) ? string.Empty : $"{Forename[0]}."
                : Forename;
            var surname = string.IsNullOrWhiteSpace(Surname) ? string.Empty : Surname;
            return $"{forename} {callsign}{surname} (id={Id})";
        }

        public MemberStatus Status
        {
            get => Get<MemberStatus>();
            set => Set(value, true);
        }

        public Member(string id, string forename, string? surname, MemberStatus status)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Forename = forename;
            Surname = surname;
            Status = status;
        }
    }
}
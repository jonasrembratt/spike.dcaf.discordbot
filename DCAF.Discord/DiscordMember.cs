using System;
using System.Text.Json.Serialization;
using DCAF.Model;
using TetraPak.XP.DynamicEntities;
using TetraPak.XP.Serialization;

namespace DCAF.Discord
{
    [JsonConverter(typeof(DynamicEntityJsonConverter<Member>))]
    [JsonKeyFormat(KeyTransformationFormat.None)]
    public sealed class DiscordMember : Member
    {
        public DiscordName DiscordName
        {
            get => Get<DiscordName>()!; 
            set => Set(value);
        }

        public DiscordMember(string id, DateTime dateOfApplication, string forename, string? surname, MemberGrade grade, MemberStatus status) 
        : base(id, dateOfApplication, forename, surname, grade, status) 
        {
        }
    }
    
    
}
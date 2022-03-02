using System.Text.Json.Serialization;
using DCAF.DiscordBot.Model;
using DCAF.Model;
using TetraPak.XP.DynamicEntities;
using TetraPak.XP.Serialization;

namespace DCAF.Discord
{
    [JsonConverter(typeof(DynamicEntityJsonConverter<Member>))]
    [JsonKeyFormat(KeyTransformationFormat.None)]
    public class DiscordMember : Member
    {
        public DiscordName DiscordName
        {
            get => Get<DiscordName>()!; 
            set => Set(value);
        }

        public DiscordMember(string id, string forename, string? surname, MemberStatus status) 
        : base(id, forename, surname, status) 
        {
        }
    }
}
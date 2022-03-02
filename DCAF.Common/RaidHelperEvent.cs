using System.Text.Json.Serialization;
using TetraPak.XP.DynamicEntities;

namespace DCAF.DiscordBot.Model
{
    public class RaidHelperEvent 
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("date")]
        public string Date { get; set; }
        
        [JsonPropertyName("signups")]
        public RaidHelperEventSignup[] Signups { get; set; }
        
        [JsonPropertyName("raidid")]
        public string RaidId { get; set; }
    }

    [JsonConverter(typeof(DynamicEntityJsonConverter<RaidHelperEventSignup>))]
    public class RaidHelperEventSignup : DynamicEntity
    {
        [JsonPropertyName("role")]
        public string Role
        {
            get => Get<string>();
            set => Set(value);
        }
        
        [JsonPropertyName("name")]
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }
        
        [JsonPropertyName("userid")]
        public string UserId
        {
            get => Get<string>();
            set => Set(value);
        }

    }
}
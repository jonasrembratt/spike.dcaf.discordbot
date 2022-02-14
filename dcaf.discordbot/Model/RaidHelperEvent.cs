using System.Text.Json.Serialization;

namespace DCAF.DiscordBot.Model
{
    public class RaidHelperdEvent : Entity
    {
        [JsonPropertyName("userid")]
        public string UserId { get; set; }
    }
}
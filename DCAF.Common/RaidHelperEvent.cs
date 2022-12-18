using System.Text;
using System.Text.Json.Serialization;
using TetraPak.XP;
using TetraPak.XP.DynamicEntities;

namespace DCAF
{
    public sealed class RaidHelperEvent 
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("date")]
        public string? Date { get; set; }
        
        [JsonPropertyName("signups")]
        public RaidHelperEventSignup[]? Signups { get; set; }
        
        [JsonPropertyName("raidid")]
        public string? RaidId { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{(RaidId.IsAssigned() ? RaidId! : "-no id-")}] ");
            sb.Append($"[{(Title.IsAssigned() ? Title : "(no title)")}] ");
            sb.Append($"[{(Date.IsAssigned() ? Date : "(no date)")}] ");
            sb.Append($"[{(Signups?.Length != 0 ? $"signups: {Signups!.Length.ToString()}" : "(no signups)")}] ");
            return sb.ToString();
        }

        public bool IsEmpty => RaidId is null;

        public bool IsCorrupt => IsEmpty || Date.IsUnassigned() || RaidId.IsUnassigned();
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
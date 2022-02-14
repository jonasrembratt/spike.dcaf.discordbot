using Newtonsoft.Json;
using TetraPak.XP.Configuration;
using TetraPak.XP.DynamicEntities;
using TetraPak.XP.Logging;

namespace dcaf.discordbot
{
    public class DcafConfiguration : ConfigurationSectionWrapper
    {
        public string? GuildId => Get<string?>();

        public EventsConfiguration? Events => Get<EventsConfiguration>();

        public DcafConfiguration(IConfigurationSectionExtended section, ILog? log = null) 
        : base(section, "DCAF", log)
        {
        }
    }

    [JsonConverter(typeof(DynamicEntityJsonConverter<EventsConfiguration>))]
    public class EventsConfiguration : DynamicEntity
    {
        public string? Backlog => Get<string?>();
        
        public DynamicEntity? Channels => Get<DynamicEntity?>()!;
    }
}
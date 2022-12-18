using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TetraPak.XP.Configuration;

namespace DCAF.Discord
{
    public static class DcafConfigurationHelper
    {
        public static ulong GetGuildId(this DcafConfiguration self)
        {
            if (self.GuildId == 0d)
                throw new ConfigurationException($"Missing configuration: {nameof(DcafConfiguration.GuildId)}");
            
            return self.GuildId;
        }

        public static TimeSpan GetBacklogTimeSpan(this DcafConfiguration self)
        {
            return self.Events?.Backlog ?? EventsConfiguration.DefaultBacklog;
        }

        public static TimeSpan GetRsvpTimeSpan(this DcafConfiguration self)
        {
            return self.Events?.RsvpTimeSpan ?? EventsConfiguration.DefaultRsvpTimeSpan;
        }

        public static IServiceCollection AddDcafConfiguration(this IServiceCollection collection)
        {
            collection.AddSingleton(_ => 
                new DcafConfiguration(ConfigurationSectionDecoratorArgs.ForSubSection(
                null,
                    DcafConfiguration.SectionKey)));
            return collection;
        }
    }
}
using System;
using System.Linq;
using DCAF._lib;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging;

namespace DCAF.Discord
{
    public class DcafConfiguration : ConfigurationSectionWrapper
    {
        EventsConfiguration? _events;
        PersonnelSheetConfiguration? _personnelSheet;

        public ulong GuildId => Get<ulong>();

        public DiscordName? BotName
        {
            get
            {
                var name = Get<string?>();
                return string.IsNullOrWhiteSpace(name) 
                    ? null 
                    : new DiscordName(name);
            }
        }

        public bool IsCliEnabled { get; }

        public PersonnelSheetConfiguration? PersonnelSheet 
        {
            get
            {
                if (_personnelSheet is null)
                {
                    _personnelSheet = new PersonnelSheetConfiguration(Section, nameof(PersonnelSheet));
                }
                return _personnelSheet;
            }
        }


        public EventsConfiguration? Events
        {
            get
            {
                if (_events is null)
                {
                    _events = new EventsConfiguration(Section, nameof(Events));
                }
                return _events;
            }
        }

        public DcafConfiguration(IConfiguration section, bool isCliEnabled, ILog? log = null) 
        : base(section, "DCAF", log)
        {
            IsCliEnabled = isCliEnabled;
        }
    }

    public class EventsConfiguration : ConfigurationSectionWrapper
    {
        TimeSpan? _backlog;
        TimeSpan? _rsvpTimeSpan;
        ulong[]? _channels;
        
        internal static TimeSpan DefaultBacklog { get; } = TimeSpan.FromDays(30);
        internal static TimeSpan DefaultRsvpTimeSpan { get; } = TimeSpan.FromHours(48);
        
        public TimeSpan Backlog
        {
            get
            {
                if (_backlog is { })
                    return _backlog.Value;

                var s = Get<string?>();
                if (s is null)
                {
                    _backlog = DefaultBacklog;
                }
                else if (s.TryParseTimeSpan(DateTimeHelper.Units.Hours, out var ts))
                {
                    _backlog = ts;
                }
                return _backlog!.Value;
            }
        }

        public TimeSpan RsvpTimeSpan
        {
            get
            {
                if (_rsvpTimeSpan is { })
                    return _rsvpTimeSpan.Value;

                var s = Get<string?>();
                if (s is null)
                {
                    _rsvpTimeSpan = DefaultBacklog;
                }
                else if (s.TryParseTimeSpan(DateTimeHelper.Units.Hours, out var ts))
                {
                    _rsvpTimeSpan = ts;
                }
                return _rsvpTimeSpan!.Value;
            }
        }

        public ulong[] Channels
        {

            get
            {
                if (_channels is { })
                    return _channels;
                
                var channelsSection = Get<ConfigurationSection>()!;
                var channelIds = channelsSection.Values.ToArray();
                var ulongIds = new ulong[channelIds.Length];
                for (var i = 0; i < channelIds.Length; i++)
                {
                    if (channelIds[i] is not string stringChannelId)
                        throw new ConfigurationException(
                            $"Unexpected configuration value: {new ConfigPath(Path).Push(nameof(Channels))} (#{i})"
                            +" (expected channel id as string value)");

                    if (!ulong.TryParse(stringChannelId, out var result))
                        throw new ConfigurationException(
                            $"Unexpected configuration value: {new ConfigPath(Path).Push(nameof(Channels))} (#{i})"
                            +" (expected channel id as string value)");

                    ulongIds[i] = result;
                }
                return _channels = ulongIds;
            }
        }

        public EventsConfiguration(IConfigurationSectionExtended section, ILog? log) 
        : base(section, log)
        {
        }

        public EventsConfiguration(IConfiguration? configuration, string? key, ILog? log = null) 
        : base(configuration, key, log)
        {
        }
    }

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
    }

    public class PersonnelSheetConfiguration : ConfigurationSectionWrapper
    {
        public string? SheetName => Section?.Get<string>();

        public string? ApplicationName => Section?.Get<string>();

        public string? DocumentId => Section?.Get<string>();
        
        public PersonnelSheetConfiguration(IConfiguration? configuration, string? key, ILog? log = null) 
        : base(configuration, key, log)
        {
        }
    }
    
}
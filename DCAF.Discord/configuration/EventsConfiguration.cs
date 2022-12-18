using System;
using System.Linq;
using TetraPak.XP;
using TetraPak.XP.Configuration;

namespace DCAF.Discord
{
    public sealed class EventsConfiguration : ConfigurationSectionDecorator
    {
        const string SectionKey = "Events";
        
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

                var s = this.Get<string?>();
                if (s is null)
                {
                    _backlog = DefaultBacklog;
                }
                else if (s.TryParseTimeSpan(TimeUnits.Hours, out var ts, ignoreCase:true))
                {
                    _backlog = ts;
                }
                else
                {
                    _backlog = DefaultBacklog;
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

                var s = ConfigurationSectionHelper.Get<string?>(this);
                if (s is null)
                {
                    _rsvpTimeSpan = DefaultBacklog;
                }
                else if (s.TryParseTimeSpan(TimeUnits.Hours, out var ts, ignoreCase:true))
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

                var channelsSection = this.GetSubSection("Channels"); // ConfigurationSectionHelper.Get<ConfigurationSection>(this)!;
                if (channelsSection is null)
                    return Array.Empty<ulong>();

                var channelIds = channelsSection.GetChildren().ToArray();
                var ulongIds = new ulong[channelIds.Length];
                for (var i = 0; i < channelIds.Length; i++)
                {
                    var channelId = channelIds[i].Value;
                    if (string.IsNullOrWhiteSpace(channelId))
                        throw new ConfigurationException(
                            $"Unexpected configuration value: {new ConfigPath(Path).Push(nameof(Channels))} (#{i})"
                            +" (expected channel id as string value)");

                    if (!ulong.TryParse(channelId, out var result))
                        throw new ConfigurationException(
                            $"Unexpected configuration value: {new ConfigPath(Path).Push(nameof(Channels))} (#{i})"
                            +" (expected channel id as string value)");

                    ulongIds[i] = result;
                }
                return _channels = ulongIds;
            }
        }

        public EventsConfiguration(ConfigurationSectionDecoratorArgs args) 
        : base(args)
        {
        }
    }
}
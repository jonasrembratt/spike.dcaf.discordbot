using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dcaf.discordbot;
using DCAF.DiscordBot._lib;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Model;
using Discord;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging;
using TetraPak.XP.Web.Http;

namespace DCAF.DiscordBot
{
    // todo Support caching events
    public class GuildEventsRepository
    {
        const ulong RaidHelperId = 579155972115660803;

        readonly List<EventsCollection> _events = new();
        readonly IHttpClientProvider _httpClientProvider;
        readonly IDiscordGuild _guild;
        readonly DcafConfiguration _config;
        TimeFrame? _loadingTimeframe;
        bool _isEventsAligned;
        readonly ILog? _log;

        public Task<Outcome<GuildEvent[]>> ReadEventsAsync(
            TimeFrame timeFrame,
            CancellationToken? cancellationToken = null,
            TimeSpan? timeout = null)
        {
            cancellationToken ??= CancellationToken.None;
            timeout ??= TimeSpan.Zero;
            var expire = DateTime.Now.Add(timeout.Value);
            return Task.Run(async () =>
            {
                var result = new List<GuildEvent>();
                TimeFrame[] unloadedTimeFrames;
                lock (_events)
                {
                    if (!continueWhenLoadingOverlappingTimeFrameIsDone())
                        return Outcome<GuildEvent[]>.Fail(
                            new Exception(isCancelled() ? "Operation was cancelled" : "Operation timed out"));

                    _loadingTimeframe = timeFrame;
                    alignCachedEvents();
                    var allTimeframes = _events.Select(ec => ec.TimeFrame).ToArray();
                    var overlappingTimeframes = timeFrame.GetOverlapping(allTimeframes);
                    result.AddRange(getExistingEvents(timeFrame));
                    unloadedTimeFrames = timeFrame.Subtract(overlappingTimeframes);
                }

                try
                {
                    foreach (var loadTimeframe in unloadedTimeFrames)
                    {
                        var outcome = await loadEventsFromSourceAsync(loadTimeframe);
                        if (!outcome)
                            return Outcome<GuildEvent[]>.Fail(outcome.Exception!);

                        _events.Add(new EventsCollection(loadTimeframe, outcome.Value!));
                        result.AddRange(outcome.Value!);
                    }

                    _isEventsAligned = false;
                    return Outcome<GuildEvent[]>.Success(result.ToArray());
                }
                catch (Exception ex)
                {
                    return Outcome<GuildEvent[]>.Fail(ex);
                }
                finally
                {
                    _loadingTimeframe = null;
                }
                
                bool continueWhenLoadingOverlappingTimeFrameIsDone()
                {
                    if (_loadingTimeframe is null || _loadingTimeframe.GetOverlap(timeFrame) == Overlap.None)
                        return true;

                    
                    while (_loadingTimeframe is {} && !isCancelled() && !isExpired())
                    {
                        Task.Delay(20);
                    }

                    return !isCancelled() && !isExpired();
                }

                bool isCancelled() => cancellationToken.Value.IsCancellationRequested;

                bool isExpired() => timeout.Value != TimeSpan.Zero && DateTime.Now >= expire; 

            });
        }

        IEnumerable<GuildEvent> getExistingEvents(TimeFrame timeFrame)
        {
            var list = new List<GuildEvent>();
            foreach (var events in _events)
            {
                switch (timeFrame.GetOverlap(events.TimeFrame))
                {
                    case Overlap.None:
                        break;
                    case Overlap.Start:
                        list.AddRange(events.GetEvents(timeFrame));
                        break;
                    
                    case Overlap.Full:
                    case Overlap.End:
                        list.AddRange(events.GetEvents(timeFrame));
                        if (timeFrame.To <= events.TimeFrame.To)
                            return list;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return list;
        }

        void alignCachedEvents()
        {
            if (_isEventsAligned || !_events.Any())
                return;
            
            _events.Sort((a,b) => a.Compare(b));
            for (var i = 0; i < _events.Count-1; i++)
            {
                var a = _events[i];
                var b = _events[i+1];
                var overlap = a.TimeFrame.GetOverlap(b.TimeFrame);
                switch (overlap)
                {
                    case Overlap.None:
                        break;
                    
                    case Overlap.Full:
                    case Overlap.Start:
                    case Overlap.End:
                        a.TimeFrame = a.TimeFrame.Merge(b.TimeFrame);
                        a.AddEventsFrom(b);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _isEventsAligned = true;
        }

        async Task<Outcome<GuildEvent[]>> loadEventsFromSourceAsync(TimeFrame timeFrame)
        {
            var from = timeFrame.From;
            var to = timeFrame.To;
            var eventList = new List<GuildEvent>();
            var ct = CancellationToken.None;

            for (var c = 0; c < _config.Events!.Channels.Length; c++)
            {
                var socketGuild = await _guild.GetSocketGuildAsync();
                var channelId = _config.Events!.Channels[c];
                if (socketGuild.GetChannel(channelId) is not IMessageChannel channel)
                    throw new ConfigurationException($"Channel is not supported: {channelId}" +
                                                     $"({new ConfigPath(_config.Events!.Path).Push(nameof(EventsConfiguration.Channels))}[{c}])");

                var messages = channel.GetMessagesAsync();
                await foreach (var msgArray in messages.WithCancellation(ct))
                {
                    foreach (var message in msgArray)
                    {
                        if (!isEvent(message))
                            continue;

                        var eventPosted = message.Timestamp.DateTime;
                        if (eventPosted < from || eventPosted > to)
                            continue;

                        var rhEventOutcome = await getEventFromRaidHelper(message.Id, ct);
                        if (!rhEventOutcome)
                            return Outcome<GuildEvent[]>.Fail(rhEventOutcome.Exception!);

                        var rhEvent = rhEventOutcome.Value!;
                        var guildEvent = new GuildEvent(rhEvent, _log);
                        eventList.Add(guildEvent);
                    }
                }
            }
            return Outcome<GuildEvent[]>.Success(eventList.ToArray());
            

            bool isEvent(IMessage message) => message.Author.Id == RaidHelperId;
        }

        async Task<Outcome<RaidHelperEvent>> getEventFromRaidHelper(ulong eventId, CancellationToken cancellationToken)
        {
            var clientOutcome = await _httpClientProvider.GetHttpClientAsync();
            if (!clientOutcome)
                return Outcome<RaidHelperEvent>.Fail(
                    new Exception("Could not obtain a HTTP client (see inner exception)", clientOutcome.Exception!));

            var client = clientOutcome.Value!;
            retry:
            var response = await client.GetAsync($"https://raid-helper.dev/api/event/{eventId.ToString()}", cancellationToken);
            var retryAfter = TimeSpan.Zero;
            if (isRateLimited(response, out retryAfter) || !response.IsSuccessStatusCode)
            {
                if (retryAfter == TimeSpan.Zero)
                    return Outcome<RaidHelperEvent>.Fail(new Exception(response.ReasonPhrase));
                
                await Task.Delay(retryAfter, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                    goto retry;

                return Outcome<RaidHelperEvent>.Fail(new Exception(response.ReasonPhrase));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            try
            {
                var rhEvent = await JsonSerializer.DeserializeAsync<RaidHelperEvent>(stream, cancellationToken: cancellationToken);
                return Outcome<RaidHelperEvent>.Success(rhEvent!);
            }
            catch (Exception ex)
            {
                return Outcome<RaidHelperEvent>.Fail(ex);
            }
        }

        static bool isRateLimited(HttpResponseMessage response, out TimeSpan retryAfter)
        {
            if (response.Headers.RetryAfter is null)
            {
                retryAfter = TimeSpan.Zero;
                return false;
            }

            retryAfter = response.Headers.RetryAfter.Date!.Value.Subtract(DateTimeOffset.Now);
            if (retryAfter < TimeSpan.Zero)
            {
                retryAfter = TimeSpan.FromMilliseconds(10);
            }
            return true;
        }
        
        // static ulong[] getEventsChannels(DcafConfiguration config) obsolete
        // {
        //     if (!config.Events?.Channels.Any() ?? true)
        //         throw new InvalidOperationException($"No {nameof(DcafConfiguration.Events)} or channels is configured");
        //
        //     return config.Events.Channels.ToArray();
        // }
        
        public GuildEventsRepository(
            DcafConfiguration dcafConfiguration, 
            IDiscordGuild guild, 
            IHttpClientProvider httpClientProvider,
            ILog? log)
        {
            _config = dcafConfiguration;
            _guild = guild;
            _httpClientProvider = httpClientProvider;
            _log = log;
            var backlog = dcafConfiguration.GetBacklogTimeSpan();
            var timeframe = new TimeFrame(DateTime.UtcNow.Subtract(backlog), DateTime.UtcNow);
            ReadEventsAsync(timeframe);
        }
    }
    
    class EventsCollection
    {
        readonly List<GuildEvent> _events = new();

        public TimeFrame TimeFrame { get; set; }

        internal int Compare(EventsCollection other)
        {
            return TimeFrame.From < other.TimeFrame.From
                ? -1
                : TimeFrame.From > other.TimeFrame.From
                    ? 1
                    : 0; 
        }

        internal GuildEvent[] GetEvents(TimeFrame timeFrame) 
            => 
            _events.Where(e => e.Date >= timeFrame.From && e.Date <= timeFrame.To).ToArray();

        public void AddEventsFrom(EventsCollection source)
        {
            _events.AddRange(source._events);
            _events.Sort((a,b) => a.Date < b.Date ? -1 : a.Date > b.Date ? 1 : 0);
        }

        public EventsCollection(TimeFrame timeFrame, IEnumerable<GuildEvent> events)
        {
            TimeFrame = timeFrame;
            _events.AddRange(events);
        }
    }
}
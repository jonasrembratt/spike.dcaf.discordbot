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
using TetraPk.XP.Web.Http;

namespace DCAF.DiscordBot
{
    // todo Support caching events
    public class GuildEventsRepository
    {
        const ulong RaidHelperId = 579155972115660803;

        readonly List<EventsCollection> _events = new();
        readonly IHttpClientProvider _httpClientProvider;
        readonly IDiscordGuild _guild;
        readonly ulong[] _configuredChannels;
        TaskCompletionSource<Outcome<GuildEvent[]>> _loadingTcs = new();
        bool _isEventsAligned;

        public Task<Outcome<GuildEvent[]>> ReadEventsAsync(TimeFrame timeframe)
        {
            Task.Run(async () =>
            {
                var result = new List<GuildEvent>();
                TimeFrame[] unloadedTimeFrames;
                lock (_events)
                {
                    _loadingTcs.AwaitResult(); // awaits ongoing operation
                    alignCachedEvents();
                    var allTimeframes = _events.Select(ec => ec.TimeFrame).ToArray();
                    var overlappingTimeframes = timeframe.GetOverlapping(allTimeframes);
                    result.AddRange(getExistingEvents(timeframe));
                    unloadedTimeFrames = timeframe.Subtract(overlappingTimeframes);
                    _loadingTcs = new TaskCompletionSource<Outcome<GuildEvent[]>>();
                }

                try
                {
                    foreach (var loadTimeframe in unloadedTimeFrames)
                    {
                        var outcome = await loadEventsFromSourceAsync(loadTimeframe, _configuredChannels);
                        if (!outcome)
                        {
                            _loadingTcs.SetResult(outcome);
                            return;
                        }

                        _events.Add(new EventsCollection(loadTimeframe, outcome.Value!));
                        result.AddRange(outcome.Value!);
                    }
                    
                    _isEventsAligned = false;
                    _loadingTcs.SetResult(Outcome<GuildEvent[]>.Success(result.ToArray()));
                }
                catch (Exception ex)
                {
                    _loadingTcs.SetException(ex);
                }
            });

            return _loadingTcs.Task;
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
            if (_isEventsAligned)
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

        // void addTimeframe(TimeFrame timeFrame, Outcome<GuildEvent[]> outcome)
        // {
        //     if (!outcome)
        //         return;
        //
        //     var events = outcome.Value!;
        //     foreach (var evt in events)
        //     {
        //         evt.Date
        //     }
        // }

        async Task<Outcome<GuildEvent[]>> loadEventsFromSourceAsync(TimeFrame timeFrame, ulong[] channels)
        {
            var from = timeFrame.From;
            var to = timeFrame.To;
            var eventList = new List<GuildEvent>();
            var ct = CancellationToken.None;

            for (var c = 0; c < channels.Length; c++)
            {
                var channel = _guild.SocketGuild.GetChannel(channels[c]) as IMessageChannel;
                var messages = channel!.GetMessagesAsync();
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
                        var guildEvent = new GuildEvent(rhEvent);
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
            if (!response.IsSuccessStatusCode || isRateLimited(response, out retryAfter))
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
        
        static TimeSpan resolveEventsTimeframe(DcafConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Events?.Backlog))
                return TimeSpan.FromDays(30);

            return config.Events!.Backlog!.TryParseTimeSpan(DateTimeHelper.Units.DaysIdent, out var timeSpan)
                ? timeSpan
                : throw new FormatException($"Unrecognized events backlog configuration: {config.Events!.Backlog!}");
        }

        static ulong[] getEventsChannels(DcafConfiguration config)
        {
            if (!config.Events?.Channels?.Any() ?? true)
                throw new InvalidOperationException($"No {nameof(DcafConfiguration.Events)} or channels is configured");

            return config.Events.Values.Where(i => i is ulong).Cast<ulong>().ToArray();
        }
        
        public GuildEventsRepository(DcafConfiguration dcafConfiguration, IDiscordGuild guild, IHttpClientProvider httpClientProvider)
        {
            _guild = guild;
            _httpClientProvider = httpClientProvider;
            var backlog = resolveEventsTimeframe(dcafConfiguration);
            var timeframe = new TimeFrame(DateTime.UtcNow.Subtract(backlog), DateTime.UtcNow);
            _configuredChannels = getEventsChannels(dcafConfiguration);
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
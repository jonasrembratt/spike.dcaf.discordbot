using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DCAF._lib;
using DCAF.DiscordBot.Model;
using DCAF.Model;
using Discord;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging;
using TetraPak.XP.Web.Http;

namespace DCAF.Discord
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
            TimeSpan? timeout = null,
            bool breakOnFailure = true)
        {
            cancellationToken ??= CancellationToken.None;
            DateTime? expire = timeout is { } ? DateTime.Now.Add(timeout.Value) : null;
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
                        var outcome = await loadEventsFromSourceAsync(loadTimeframe, breakOnFailure, timeout);
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

                bool isExpired() => expire is {} && DateTime.Now >= expire; 

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

        async Task<Outcome<GuildEvent[]>> loadEventsFromSourceAsync(TimeFrame timeFrame, bool breakOnFailure, TimeSpan? timeout)
        {
            var eventList = new List<GuildEvent>();
            var ct = CancellationToken.None;
            
            for (var c = 0; c < _config.Events!.Channels.Length; c++)
            {
                var socketGuild = await _guild.GetSocketGuildAsync();
                var channelId = _config.Events!.Channels[c];
                if (socketGuild.GetChannel(channelId) is not IMessageChannel channel)
                    throw new ConfigurationException($"Channel is not supported: {channelId}" +
                                                     $"({new ConfigPath(_config.Events!.Path).Push(nameof(EventsConfiguration.Channels))}[{c}])");

                var eventsOutcome = await channel.GetFilteredMessagesAsync(e =>
                    {
                        var events = e.Downloaded.Where(m => m.IsInTimeFrame(timeFrame) && isEvent(m)).ToArray();
                        var oldest = e.Downloaded[e.Downloaded.Length - 1];
                        if (oldest.Timestamp < timeFrame.From)
                        {
                            e.Done(events);
                        }
                        else
                        {
                            e.ReadMoreBefore(events, oldest);
                        }
                    }, 
                    timeout,
                    ct);
                if (!eventsOutcome && breakOnFailure)
                    return Outcome<GuildEvent[]>.Fail(eventsOutcome.Exception!);

                foreach (var evt in eventsOutcome.Value!)
                {
                    var rhEventOutcome = await getEventFromRaidHelper(evt.Id, ct);
                    if (!rhEventOutcome)
                        return Outcome<GuildEvent[]>.Fail(rhEventOutcome.Exception!);

                    var rhEvent = rhEventOutcome.Value!;
                    var guildEvent = new GuildEvent(rhEvent, _log);
                    eventList.Add(guildEvent);
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
            if (isRateLimited(response, out var retryAfter) || !response.IsSuccessStatusCode)
            {
                if (retryAfter == TimeSpan.Zero)
                    return Outcome<RaidHelperEvent>.Fail(new Exception(response.ReasonPhrase));
                
                await Task.Delay(retryAfter, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                    goto retry;

                return Outcome<RaidHelperEvent>.Fail(new Exception(response.ReasonPhrase));
            }

#if NET5_0_OR_GREATER            
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
            using var stream = await response.Content.ReadAsStreamAsync();
#endif

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

    static class MessageChannelHelper
    {
        public static bool IsInTimeFrame(this IMessage message, TimeFrame timeFrame)
        {
            var eventPosted = message.Timestamp.DateTime;
            return eventPosted >= timeFrame.From && eventPosted <= timeFrame.To;
        }

        public static async Task<Outcome<IMessage[]>> GetFilteredMessagesAsync(
            this IMessageChannel channel, 
            ReadFilteredMessageDelegate filterDelegate,
            TimeSpan? timeout = null,
            CancellationToken? cancelToken = null)
        {
            var direction = Direction.Before;
            var messages = new List<IMessage>();
            var done = false;
            var limitReached = false;
            var limit = 20;
            int? setLimit = null;
            var ct = cancelToken ?? CancellationToken.None;
            IMessage? fromMessage = null;
            var options = new RequestOptions
            {
                AuditLogReason = "DCAF bot reads events",
                CancelToken = ct,
                RetryMode = RetryMode.AlwaysRetry,
                RatelimitCallback = onRateLimit
            };

            DateTime? expires = timeout is {} ? DateTime.Now.Add(timeout.Value) : null;
            while (!done && !isExpiredOrCancelled())
            {
                if (limitReached)
                {
                    limitReached = false;
                    await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
                    if (isExpiredOrCancelled())
                        continue;
                }
                if (setLimit.HasValue)
                {
                    limit = setLimit.Value;
                }

                setLimit = null;
                limitReached = false;
                try
                {
                    var readMessages = fromMessage is null
                        ? await channel.GetMessagesAsync(limit, options:options).FlattenAsync()
                        : await channel.GetMessagesAsync(
                                fromMessage, 
                                direction, 
                                limit, 
                                options: options)
                            .FlattenAsync();
                    var msgArray = readMessages?.ToArray() ?? Array.Empty<IMessage>();
                    var args = new ReadMessagesFilterArgs(msgArray, limit);
                    filterDelegate(args);
                    messages.AddRange(args.Filtered);
                    done = args.IsDone;
                    if (done)
                        continue;

                    fromMessage = args.FromMessage;
                    direction = args.Direction;
                    if (args.ReadLimit.HasValue && args.ReadLimit.Value < limit)
                    {
                        limit = args.ReadLimit.Value;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }

            return isExpiredOrCancelled() 
                ? Outcome<IMessage[]>.Fail("Reading messages was cancelled or timed out") 
                : Outcome<IMessage[]>.Success(messages.ToArray());

            bool isExpiredOrCancelled() => DateTime.Now >= expires || ct.IsCancellationRequested;

            Task onRateLimit(IRateLimitInfo info)
            {
                limitReached = info.Remaining == 0;
                if (info.Limit.HasValue && info.Limit.Value < limit)
                {
                    setLimit = info.Limit.Value;
                }
                
                return Task.CompletedTask;
            }
        }
    }

    public delegate void ReadFilteredMessageDelegate(ReadMessagesFilterArgs args);
    
    public class ReadMessagesFilterArgs
    {
        public IMessage[] Downloaded { get; }

        public IMessage[] Filtered { get; private set; }

        internal IMessage FromMessage { get; private set; }

        internal Direction Direction { get; private set; }

        public int? ReadLimit { get; private set; }

        internal bool IsDone { get; private set; }

        public void ReadMoreBefore(IMessage[] filtered, IMessage fromMessage, int? readLimit = null)
        {
            Filtered = filtered;
            IsDone = false;
            FromMessage = fromMessage;
            Direction = Direction.Before;
            ReadLimit = readLimit;
        }

        public void ReadMoreAfter(IMessage[] filtered, IMessage fromMessage, int? readLimit = null)
        {
            Filtered = filtered;
            IsDone = false;
            FromMessage = fromMessage;
            Direction = Direction.After;
            ReadLimit = readLimit;
        }

        public void ReadMoreAround(IMessage[] filtered, IMessage fromMessage, int? readLimit = null)
        {
            Filtered = filtered;
            IsDone = false;
            FromMessage = fromMessage;
            Direction = Direction.Around;
            ReadLimit = readLimit;
        }

        public void Done(IMessage[] filtered)
        {
            IsDone = true;
            Filtered = filtered;
        }

        public ReadMessagesFilterArgs(IMessage[] messages, int limit)
        {
            Filtered = Array.Empty<IMessage>();
            Direction = Direction.Before;
            Downloaded = messages;
            IsDone = true;
            FromMessage = messages[messages.Length - 1];
            ReadLimit = limit;
        }
    }
}
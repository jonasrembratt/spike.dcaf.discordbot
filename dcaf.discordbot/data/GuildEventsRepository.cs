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
using Discord;
using TetraPk.XP.Web.Http;

namespace DCAF.DiscordBot.Model
{
    // todo Support caching events
    public class GuildEventsRepository
    {
        const ulong RaidHelperId = 579155972115660803;
        
        readonly DcafConfiguration _dcafConfiguration;
        readonly IHttpClientProvider _httpClientProvider;
        readonly TaskCompletionSource<Outcome<GuildEvent[]>> _loadingTcs = new();
        readonly IDiscordGuild _guild;
        

        void loadEventsAsync(TimeFrame timeframe, ulong[] channels)
        {
            Task.Run(async () =>
            {
                try
                {
                    _loadingTcs.SetResult(await getEventsAsync(timeframe, channels));
                }
                catch (Exception ex)
                {
                    _loadingTcs.SetException(ex);
                }
            });
        }
        
        async Task<Outcome<GuildEvent[]>> getEventsAsync(TimeFrame timeFrame, ulong[] channels)
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

        public GuildEventsRepository(DcafConfiguration dcafConfiguration, IDiscordGuild guild, IHttpClientProvider httpClientProvider)
        {
            _dcafConfiguration = dcafConfiguration;
            _guild = guild;
            _httpClientProvider = httpClientProvider;
            var backlog = resolveEventsTimeframe(dcafConfiguration);
            var timeframe = new TimeFrame(DateTime.UtcNow.Subtract(backlog), DateTime.UtcNow);
            var channels = getEventsChannels(dcafConfiguration);
            loadEventsAsync(timeframe, channels);
        }

        static ulong[] getEventsChannels(DcafConfiguration config)
        {
            if (!config.Events?.Channels?.Any() ?? true)
                throw new InvalidOperationException($"No {nameof(DcafConfiguration.Events)} or channels is configured");

            return config.Events.Values.Where(i => i is ulong).Cast<ulong>().ToArray();
        }

        public async Task<Outcome<GuildEvent[]>> ReadEventsAsync(TimeFrame? timeFrame)
        {
            throw new NotImplementedException();
        }
    }
}
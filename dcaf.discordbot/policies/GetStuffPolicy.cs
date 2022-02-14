using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Model;
using Discord;
using TetraPk.XP.Web.Http;

namespace DCAF.DiscordBot.Policies
{
    public class GetStuffPolicy : Policy
    {
        const string CommandName = "get";
        const string ArgKeyRsvp = "--rsvp";
        // readonly IDiscordGuild _guild; obsolete
        // static readonly DiscordName RaidHelperName = new DiscordName("Raid-Helper", "3806");
        // const ulong RaidHelperId = 579155972115660803;
        // HttpClient? _httpClient;
        //
        // const ulong FirstAegEventsChannelId = 706228440604344452; 
        // const ulong SecondAegEventsChannelId = 900174498047877142;
        // const ulong JointOpsEventsChannelId = 893973970985041920;
        // const ulong TrainingEventsChannelId = 800182082324267059;

        Action<IEnumerable<RaidHelperEvent>>? _getEventsCallback;
        readonly IHttpClientProvider _httpClientProvider;
        readonly GuildEventsRepository _eventsRepository;

        public override async Task<Outcome> ExecuteAsync(PolicyArgs e)
        {
            var args = e.Parameters;
            if (!args.Any())
                return Outcome.Fail(new Exception("Expected entity type"));

            var entityType = args[0].ToLowerInvariant();
            switch (entityType)
            {
                case "events":
                    var timeFrameOutcome = e.Parameters.GetTimeFrame(new TimeFrame(DateTime.MinValue, DateTime.UtcNow));
                    if (!timeFrameOutcome)
                        return Outcome.Fail(timeFrameOutcome.Exception!);

                    var timeFrame = timeFrameOutcome.Value!;
                    var rsvp = TimeSpan.Zero;
                    if (e.Parameters.TryGetValue(out var rsvpStringValue, ArgKeyRsvp))
                    {
                        if (!rsvpStringValue.TryParseTimeSpan(DateTimeHelper.Units.HoursIdent, out rsvp))
                            return Outcome.Fail(new Exception($"Invalid RSVP value: {rsvpStringValue}"));

                        timeFrame = timeFrame.Subtract(rsvp);
                    }

#pragma warning disable CS4014
                    Task.Run(async () => respondGetEvent(await _eventsRepository.ReadEventsAsync(timeFrame), e));
#pragma warning restore CS4014
                    
                    
// #pragma warning disable CS4014 obsolete
//                     // todo get events channels from configuration / CLI args
//                     Task.Run(() => getEventsAsync(timeFrame, e, new ulong[]
//                     {
//                         FirstAegEventsChannelId,
//                         SecondAegEventsChannelId,
//                         JointOpsEventsChannelId,
//                         TrainingEventsChannelId
//                     }));
// #pragma warning restore CS4014
                     return Outcome.Success("Fetching events. Response pending ...");
                
                default:
                    return Outcome.Fail(new Exception($"Unsupported entity type: {entityType}"));
            }
        }

        // async Task getEventsAsync(TimeFrame timeFrame, PolicyArgs e, ulong[] channels) obsolete
        // {
        //     var from = timeFrame.From;
        //     var to = timeFrame.To;
        //     var eventList = new List<GuildEvent>();
        //
        //     try
        //     {
        //         for (var c = 0; c < channels.Length; c++)
        //         {
        //             var channel = _guild.SocketGuild.GetChannel(channels[c]) as IMessageChannel;
        //             var messages = channel!.GetMessagesAsync();
        //             Console.WriteLine($"Got {await messages.CountAsync()} messages from {channel.Name} ..."); // nisse
        //             await foreach (var msgArray in messages)
        //             {
        //                 foreach (var message in msgArray)
        //                 {
        //                     if (!isEvent(message))
        //                         continue;
        //
        //                     var eventPosted = message.Timestamp.DateTime;
        //                     if (eventPosted < from || eventPosted > to)
        //                     {
        //                         Console.WriteLine($"  (not in time frame: {eventPosted:u})"); // nisse
        //                         continue;
        //                     }
        //
        //                     Console.WriteLine($"Getting RH event ..."); // nisse
        //                     var rhEventOutcome = await getEventFromRaidHelper(message.Id, e.CancellationToken);
        //                     if (!rhEventOutcome)
        //                     {
        //                         Console.WriteLine($"Getting RH event ... FAIL: {rhEventOutcome.Exception}"); // nisse
        //                         respondGetEvent(Outcome<GuildEvent[]>.Fail(rhEventOutcome.Exception!), e);
        //                         return;
        //                     }
        //
        //                     var rhEvent = rhEventOutcome.Value!;
        //                     if (rhEvent.RaidId is null)
        //                     {
        //                         Console.WriteLine("nisse"); // nisse
        //                     }
        //                     Console.WriteLine($"Getting RH event ... OK: {rhEvent} ... converting to GuildEvent ..."); // nisse
        //                     var guildEvent = new GuildEvent(rhEvent);
        //                     eventList.Add(guildEvent);
        //                     Console.WriteLine($"  {guildEvent}"); // nisse
        //                 }
        //             }
        //         }
        //         respondGetEvent(Outcome<GuildEvent[]>.Success(eventList.ToArray()), e);
        //     }
        //     catch (Exception ex)
        //     {
        //         respondGetEvent(Outcome<GuildEvent[]>.Fail(ex), e);
        //     }
        //
        //     bool isEvent(IMessage message) => message.Author.Id == RaidHelperId;
        // }

        static void respondGetEvent(Outcome<GuildEvent[]> outcome, PolicyArgs e)
        {
            if (e.IsCliMessage)
            {
                if (!outcome)
                {
                    Console.WriteLine($"Error fetching events. {outcome.Message}");
                    return;
                }

                Console.WriteLine("Events found:");
                for (var i = 0; i < outcome.Value!.Length; i++)
                {
                    var guildEvent = outcome.Value![i];
                    Console.WriteLine(guildEvent.ToString());
                }
                return;
            }

            throw new NotImplementedException(); // todo Send events as message to requester
        }

        // async Task<Outcome<RaidHelperEvent>> getEventFromRaidHelper(ulong eventId, CancellationToken cancellationToken) obsolete
        // {
        //     var clientOutcome = await _httpClientProvider.GetHttpClientAsync();
        //     if (!clientOutcome)
        //         return Outcome<RaidHelperEvent>.Fail(
        //             new Exception("Could not obtain a HTTP client (see inner exception)", clientOutcome.Exception!));
        //
        //     var client = clientOutcome.Value!;
        //     retry:
        //     var response = await client.GetAsync($"https://raid-helper.dev/api/event/{eventId.ToString()}", cancellationToken);
        //     var retryAfter = TimeSpan.Zero;
        //     if (!response.IsSuccessStatusCode || isRateLimited(response, out retryAfter))
        //     {
        //         if (retryAfter == TimeSpan.Zero)
        //             return Outcome<RaidHelperEvent>.Fail(new Exception(response.ReasonPhrase));
        //         
        //         await Task.Delay(retryAfter, cancellationToken);
        //         if (!cancellationToken.IsCancellationRequested)
        //             goto retry;
        //
        //         return Outcome<RaidHelperEvent>.Fail(new Exception(response.ReasonPhrase));
        //     }
        //
        //     await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        //
        //     try
        //     {
        //         var rhEvent = await JsonSerializer.DeserializeAsync<RaidHelperEvent>(stream, cancellationToken: cancellationToken);
        //         return Outcome<RaidHelperEvent>.Success(rhEvent!);
        //     }
        //     catch (Exception ex)
        //     {
        //         return Outcome<RaidHelperEvent>.Fail(ex);
        //     }
        // }

        // bool isRateLimited(HttpResponseMessage response, out TimeSpan retryAfter)
        // {
        //     if (response.Headers.RetryAfter is null)
        //     {
        //         retryAfter = TimeSpan.Zero;
        //         return false;
        //     }
        //
        //     retryAfter = response.Headers.RetryAfter.Date!.Value.Subtract(DateTimeOffset.Now);
        //     if (retryAfter < TimeSpan.Zero)
        //     {
        //         retryAfter = TimeSpan.FromMilliseconds(10);
        //     }
        //     return true;
        // }

        // HttpClient getHttpClient()
        // {
        //     if (_httpClient is { })
        //         return _httpClient;
        //
        //     _httpClient = new HttpClient();
        //     return _httpClient;
        // }
        
        public override Task<Outcome> ResetCacheAsync() => Task.FromResult(Outcome.Success());

        public GetStuffPolicy(PolicyDispatcher dispatcher, GuildEventsRepository eventsRepository/* obsolete IDiscordGuild guild, IHttpClientProvider httpClientProvider*/) 
        : base(CommandName, dispatcher)
        {
            _eventsRepository = eventsRepository;
            // _guild = guild; obsolete
            // _httpClientProvider = httpClientProvider;
        }
    }
}
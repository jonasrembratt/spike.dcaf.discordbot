// using System;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using DCAF.DiscordBot._lib;
// using DCAF.DiscordBot.Model;
// using TetraPak.XP.Logging;
//
// namespace DCAF.DiscordBot.Policies
// {
//     public class GetStuffPolicy : Policy<>
//     {
//         const string CommandName = "get";
//         const string ArgKeyRsvp = "--rsvp";
//         readonly GuildEventsRepository _eventsRepository;
//
//         public override async Task<Outcome> ExecuteAsync(PolicyArgs e)
//         {
//             var args = e.Parameters;
//             if (!args.Any())
//                 return Outcome.Fail(new Exception("Expected entity type"));
//
//             var entityType = args[0].ToLowerInvariant();
//             switch (entityType)
//             {
//                 case "events":
//                     var timeFrameOutcome = e.Parameters.GetTimeFrame(new TimeFrame(DateTime.MinValue, DateTime.UtcNow));
//                     if (!timeFrameOutcome)
//                         return Outcome.Fail(timeFrameOutcome.Exception!);
//
//                     var timeFrame = timeFrameOutcome.Value!;
//                     if (e.Parameters.TryGetValue(out var rsvpStringValue, ArgKeyRsvp))
//                     {
//                         if (!rsvpStringValue.TryParseTimeSpan(DateTimeHelper.Units.Hours, out var rsvpTimeSpan))
//                             return Outcome.Fail(new Exception($"Invalid RSVP value: {rsvpStringValue}"));
//
//                         timeFrame = timeFrame.Subtract(rsvpTimeSpan);
//                     }
//
// #pragma warning disable CS4014
//                      Task.Run(async () => respondGetEvent(await _eventsRepository.ReadEventsAsync(timeFrame), e));
// #pragma warning restore CS4014
//                      return Outcome.Success("Fetching events. Response pending ...");
//                 
//                 default:
//                     return Outcome.Fail(new Exception($"Unsupported entity type: {entityType}"));
//             }
//         }
//
//         void respondGetEvent(Outcome<GuildEvent[]> outcome, PolicyArgs e)
//         {
//             if (e.IsCliMessage)
//             {
//                 if (!outcome)
//                 {
//                     Log.Error(new Exception($"Error fetching events. {outcome.Message}"));
//                     return;
//                 }
//
//                 if (!(Log?.IsEnabled(LogRank.Trace) ?? false)) 
//                     return;
//                 
//                 var sb = new StringBuilder();
//                 sb.AppendLine("Events found:");
//                 for (var i = 0; i < outcome.Value!.Length; i++)
//                 {
//                     var guildEvent = outcome.Value![i];
//                     sb.AppendLine(guildEvent.ToString());
//                 }
//                 Log.Trace(sb.ToString);
//                 return;
//             }
//
//             throw new NotImplementedException(); // todo Send events as message to requester
//         }
//         
//         public override Task<Outcome> ResetCacheAsync() => Task.FromResult(Outcome.Success());
//
//         public GetStuffPolicy(
//             PolicyDispatcher dispatcher, 
//             GuildEventsRepository eventsRepository, 
//             // DcafConfiguration config, obsolete
//             ILog? log) 
//         : base(CommandName, dispatcher, log)
//         {
//             _eventsRepository = eventsRepository;
//             // _defaultRsvpTimeSpan = config.GetRsvpTimeSpan(); obsolete
//         }
//
//     }
// }
// using System.Threading.Tasks;
// using DCAF.Discord.Conversations;
// using Discord.Interactions;
//
// namespace DCAF.Discord.Commands;
//
// public sealed class MemberApplicationsCommands : InteractionModuleBase<SocketInteractionContext>
// {
//     readonly InteractionService _interactionService;
//     readonly InteractionHandler _interactionHandler;
//     readonly ConversationManager _conversationManager;
//
//     internal const string JoinDcafPilotModalId = "_join_dcaf_pilot";
//     internal const string JoinDcafControllerModalId = "_join_dcaf_contoller";
//
//     const string CareerPilotText = "Pilot Officer Candidate";
//     const string CareerPilotValue = "pilot";
//     const string CareerControllerText = "Controller Officer Candidate";
//     const string CareerControllerValue = "controller";
//
//     //[SlashCommand("join", "Apply for DCAF membership and training")]
//     public Task JoinDcafAsync()
//     {
//         _conversationManager.StartConversationAsync(new JoinDcafApplication(Context));
//         return Task.CompletedTask;
//     }
//
//     public MemberApplicationsCommands(
//         InteractionService interactionService, 
//         InteractionHandler interactionHandler,
//         ConversationManager conversationManager)
//     {
//         _interactionService = interactionService;
//         _interactionHandler = interactionHandler;
//         _conversationManager = conversationManager;
//     }
// }
//
//
//

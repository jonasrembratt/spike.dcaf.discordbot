using System;
using System.Collections.Generic;
using Discord;
using Discord.Interactions;

namespace DCAF.Discord.Conversations;

// sealed class JoinDcafApplication : Conversation
// {
//     public ulong? DiscordId { get; set; }
//
//     public DiscordName? DiscordName { get; set; }
//
//     public string? Forename { get; set; }
//
//     public string? Lastname { get; set; }
//
//     public string? EmailAddress { get; set; }
//
//     public string? SpokenLanguages { get; set; }
//     
//     public string? TimeZone { get; set; }
//
//     public string? PriorSquads { get; set; }
//
//     public DateTime? DateOfBirth { get; set; }
//
//     public string? PrimaryChoice { get; set; }
//
//     public string? SecondaryChoice { get; set; }
//     
//     public JoinDcafApplication(SocketInteractionContext context)
//     : base(context)
//     {
//     }
// }


// class JoinDcaf
// {
//     public const string ForenameKey = "forename";
//     public const string LastNameKey = "lastname";
//
//     protected static ModalBuilder Builder(string id, string title) => new ModalBuilder()
//         .WithCustomId(id)
//         .WithTitle(title)
//         .AddTextInput("Forename", ForenameKey)
//         .AddTextInput("Surname", LastNameKey);
//
// }
//
// class JoinDcafAsPilot : JoinDcaf
// {
//     public const string ModalKey = "join_dcaf_pilot";
//     public const string PlatformOptionsKey = "platform";
//
//     internal static Modal Build()
//     {
//         
//         var platformMenu = new SelectMenuBuilder()
//             .WithCustomId(PlatformOptionsKey)
//             .WithOptions(new List<SelectMenuOptionBuilder>
//             {
//                 new("F-14B Tomcat", "f14b", "Train as pilot and/or RIO in the legendary Tomcat!"),
//                 new("F/A-18C Hornet", "f18c", "Train to become a multi role naval aviator"),
//                 new("AV-8B Night Attack V/STOL", "av8b", "Become an attack ace in this awesome platform"),
//                 new("F-16C Viper", "f16c", "Go all the way as an elite wild weasel pilot"),
//             }).Build();
//
//         var builder = Builder(ModalKey, "Apply for DCAF pilot training")
//             .AddComponents(new List<IMessageComponent> { platformMenu }, 1);
//         return builder.Build();
//     }
// }
//
// class JoinDcafAsController : JoinDcaf
// {
//     public const string ModalKey = "join_dcaf_controller";
//
//     internal static Modal Build()
//     {
//         var builder = Builder(ModalKey, "Apply for DCAF controller training");
//         return builder.Build();
//     }
// }
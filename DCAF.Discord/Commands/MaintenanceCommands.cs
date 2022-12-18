using System;
using System.Threading.Tasks;
using DCAF.Discord.Policies;
using DCAF.Discord.Scheduling;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using TetraPak.XP;
using TetraPak.XP.DependencyInjection;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Commands;

// todo Make separate policies for maintenance actions and use them instead, just like with "sync ids" and "update member status" ('classic' commands)
public sealed class MaintenanceCommands : InteractionModuleBase<SocketInteractionContext>
{
    readonly PolicyDispatcher _policyDispatcher;

    [SlashCommand("cleanup-channel", "Clean up the channel by removing messages")]
    [RequireRole("Admin")]
    public async Task CleanChannelAsync(
        [Summary("except", "Exempt messages from a specified role or member")]
        IMentionable? except = null,
        [Summary("min-age", "Specifies a minimum age for messages to be removed (eg. 4h = 4 hours, 4d = 4 days, etc.)")]
        string? minAge = null,
        [Summary("max-age", "Specifies a maximum age for messages to be removed (eg. 4h = 4 hours, 4d = 4 days, etc.)")]
        string? maxAge = null,
        [Summary("includePinned", "Also removes pinned messages")]
        bool includePinned = false,
        [Summary("silent", "Set to suppress outgoing messages from the clean up policy")]
        bool silent = false
#if DEBUG        
        ,
        [Summary("simulate", "For testing/development: Performs all steps of cleanup without deleting posts")]
        bool simulate = false
#endif        
    )
    {
#if !DEBUG
        // ReSharper disable once InconsistentNaming
        const bool simulate = false;
#endif
        var log = XpServices.Get<ILog>();
        log.Information($"Runs channel cleanup on #{Context.Channel.Name}");
        await RespondAsync("Initiates channel cleanup ...");
        _ = Task.Run(async () =>
        {
            if (!_policyDispatcher.TryGetPolicy<CleanupChannelPolicy>(out var policy))
            {
                log.Error(new Exception( $"Could not obtain {typeof(CleanupChannelPolicy)} from policy dispatcher"));
                return;
            }                
                
            var exceptStringId = except switch
            {
                null => null,
                SocketGuildUser socketGuildUser => socketGuildUser.Id.ToString(),
                _ => except.ToString()
            };

            var args = new CleanupChannelArgs(Context.Channel.Id, exceptStringId, minAge, maxAge, includePinned, simulate, silent);
            var outcome = await policy!.ExecuteAsync(args);
            var policyOutcome = new PolicyOutcomeArgs(policy, outcome, Context.Channel, null);
            await onCleanupChannelOutcome(policyOutcome);
        });
    }

    static async Task onCleanupChannelOutcome(PolicyOutcomeArgs e)
    {
        if (e.Outcome is not Outcome<CleanupChannelResult> outcome)
            return;

        if (outcome.Value?.Silent ?? false)
            return;
            
        if (outcome)
        {
            await e.SendMessageAsync($"Cleanup channel {outcome.Value!.Channel.Name} completed: {outcome.Value!.Message}");
        }
        else
        {
            await e.SendMessageAsync($"Cleanup channel {outcome.Value!.Channel.Name} failed! {outcome.Message}");
        }

        await e.SayNextRuntime();
    }

    public MaintenanceCommands(PolicyDispatcher policyDispatcher, Scheduler scheduler)
    {
        _policyDispatcher = policyDispatcher;
        scheduler.AddPolicyOutcomeHandler(onCleanupChannelOutcome);
    }
}

static class MessageHelper
{
    public static bool IsOld(this IMessage message) =>
        message.CreatedAt < DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14));
}
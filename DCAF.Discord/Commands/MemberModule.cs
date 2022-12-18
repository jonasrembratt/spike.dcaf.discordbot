using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCAF.Discord.Policies;
using DCAF.Discord.Scheduling;
using Discord;
using Discord.Commands;
using TetraPak.XP;

namespace DCAF.Discord.Commands
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    // ReSharper disable once UnusedType.Global
    partial class DcafCommandGroup
    {
        [Group("member")]
        // ReSharper disable once UnusedType.Global
        public class MemberModule : ModuleBase<SocketCommandContext>
        {
            readonly PolicyDispatcher _policyDispatcher;

            [Command("status")]
            [Summary("Examines all members that haven't responded to roll-calls in a while and sets them as 'AWOL'")]
            public async Task MemberStatus(
                [Summary("(optional) Specifies a timespan (eg. '48h', '2w' or '30d') for allowed RSVP")]
                SetAwolArgs? args = null)
            {
                if (!_policyDispatcher.TryGetPolicy<MemberStatusPolicy>(out var policy))
                {
                    await ReplyAsync("Policy is unavailable");
                    return;
                }
                
                args ??= SetAwolArgs.Default;
                var allowedOutcome = args.GetAllowed();
                if (!allowedOutcome)
                {
                    await ReplyAsync(allowedOutcome.Message);
                    return;
                }

                var rsvpOutcome = args.GetRsvp();
                if (!rsvpOutcome)
                {
                    await ReplyAsync(rsvpOutcome.Message);
                    return;
                }

                var outcome = await policy.ExecuteAsync(null);
                await onMemberStatusOutcome(new PolicyOutcomeArgs(policy, outcome, Context.Channel, null));
            }

            static async Task onMemberStatusOutcome(PolicyOutcomeArgs e)
            {
                if (e.Outcome is not Outcome<SetMemberStatusResult> outcome)
                    return;

                if (!outcome)
                {
                    await e.SendMessageAsync(outcome.Message);
                    await e.SayNextRuntime();
                    return;
                }
                
                var result = outcome.Value!;
                await e.SendMessageAsync(result.Message);
                if (string.IsNullOrEmpty(result.DetailedMessage))
                {
                    await e.SayNextRuntime();
                    return;
                }
                
                var tempFile = new FileInfo(Path.GetTempFileName());
#if NET5_0_OR_GREATER                
                await File.WriteAllTextAsync(tempFile.FullName, result.DetailedMessage);
#else
                File.WriteAllText(tempFile.FullName, result.Message);
#endif
                var attachment = new FileAttachment(tempFile.FullName, "awol.txt");
                await e.SendFileAsync(attachment);
                await e.SayNextRuntime();
            }

            [Command("sync-ids")]
            [Summary("Ensures members in the Google sheet Personnel sheet are assigned correct Discord IDs")]
            public async Task SyncIds()
            {
                if (!_policyDispatcher.TryGetPolicy<MemberSyncIdsPolicy>(out var policy))
                {
                    await ReplyAsync("Policy is unavailable");
                    return;
                }

                var outcome = await policy.ExecuteAsync(null);
                await onMemberSyncIdOutcome(new PolicyOutcomeArgs(policy, outcome, Context.Channel, null));
            }

            static async Task onMemberSyncIdOutcome(PolicyOutcomeArgs e)
            {
                if (e.Outcome is not Outcome<SyncIdsResult> outcome)
                    return;

                const string Caption = "Members ID synchronisation was completed";
                if (!outcome)
                {
                    await e.SendMessageAsync($"{Caption} but failed when synchronising member Discord IDs. {outcome.Message}");
                    await e.SayNextRuntime();
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine(Caption);
                sb.AppendLine(outcome.Value!.UpdatedMembers.Length == 0
                    ? "All members were already synchronised"
                    : $"{outcome.Value!.UpdatedMembers.Length} members was successfully updated");
                await e.SendMessageAsync(sb.ToString());

                var result = outcome.Value!;
                if (!result.UnmatchedMembers.Any())
                {
                    await e.SayNextRuntime();
                    return;
                }

                sb.Clear();
                sb.AppendLine("The following members have Discord names in the Personnel sheet that cannot be found in Discord:");
                sb.AppendLine("--------");
                foreach (var unmatchedMember in result.UnmatchedMembers)
                {
                    sb.AppendLine(unmatchedMember.ToString());
                }
                sb.AppendLine("--------");

                if (result.UnmatchedMembers.Length < 5)
                {
                    await e.SendMessageAsync(sb.ToString());
                    await e.SayNextRuntime();
                    return;
                }

                var tempFile = new FileInfo(Path.GetTempFileName());
#if NET5_0_OR_GREATER                
                await File.WriteAllTextAsync(tempFile.FullName, sb.ToString());
#else
                File.WriteAllText(tempFile.FullName, sb.ToString());
#endif                
                var attachment = new FileAttachment(tempFile.FullName, "unmatched.txt");
                await e.SendFileAsync(attachment);
                await e.SayNextRuntime();
            }

            public MemberModule(PolicyDispatcher policyDispatcher, Scheduler scheduler)
            {
                _policyDispatcher = policyDispatcher;
                scheduler.AddPolicyOutcomeHandler(onMemberSyncIdOutcome);
                scheduler.AddPolicyOutcomeHandler(onMemberStatusOutcome);
            }
        }
    }
}
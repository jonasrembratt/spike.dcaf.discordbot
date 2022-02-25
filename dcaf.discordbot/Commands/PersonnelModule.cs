using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCAF.DiscordBot.Policies;
using Discord;
using Discord.Commands;

namespace DCAF.DiscordBot.Commands
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    // ReSharper disable once UnusedType.Global
    partial class DcafCommandGroup
    {
        [Group("personnel")]
        // ReSharper disable once UnusedType.Global
        public class PersonnelModule : ModuleBase<SocketCommandContext>
        {
            readonly PolicyDispatcher _policyDispatcher;

            const string OutputListUnmatched = "list-unmatched";

            [Command("awol")]
            [Summary("Examines all personnel that hasn't responded to roll-calls and sets them as 'AWOL'")]
            public async Task ApplyAwol(
                [Summary("(optional) Specifies a timespan (eg. '48h', '2w' or '30d') for allowed RSVP")]
                SetAwolArgs? args = null)
            {
                if (!_policyDispatcher.TryGetPolicy<AwolPolicy>(out var policy))
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

                var outcome = await policy.ExecuteAsync(args);
                if (!outcome)
                {
                    await ReplyAsync(outcome.Message);
                    return;
                }

                var result = outcome.Value!;
                if (!result.AwolMembers.Any())
                {
                    await ReplyAsync(result.Message);
                    return;
                }
                
                var sb = new StringBuilder();
                sb.AppendLine(result.Message);
                sb.AppendLine("--------");
                foreach (var awolMember in result.AwolMembers)
                {
                    sb.AppendLine(awolMember.ToString());
                }
                
                if (result.AwolMembers.Length < 5)
                {
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var tempFile = new FileInfo(Path.GetTempFileName());
                await File.WriteAllTextAsync(tempFile.FullName, sb.ToString());
                var attachment = new FileAttachment(tempFile.FullName, "awol.txt");
                await Context.Channel.SendFileAsync(attachment);
            }

            [Command("sync-ids")]
            [Summary("Ensures members in the Google sheet Personnel sheet are assigned correct Discord IDs")]
            public async Task SyncIds()
            {
                if (!_policyDispatcher.TryGetPolicy<SynchronizePersonnelIdsPolicy>(out var policy))
                {
                    await ReplyAsync("Policy is unavailable");
                    return;
                }

                var args = PolicyArgs.FromSocketMessage(Context.Message);
                var outcome = await policy.ExecuteAsync();
                if (!outcome)
                {
                    await ReplyAsync($"Failed when synchronising member Discord IDs. {outcome.Message}");
                    return;
                }

                await ReplyAsync($"{outcome.Value!.UpdatedMembers.Length} members was successfully updated");

                var result = outcome.Value!;
                if (!result.UnmatchedMembers.Any())
                    return;

                var sb = new StringBuilder();
                sb.AppendLine("These members have discord names in the Sheet that cannot be found in Discord:");
                sb.AppendLine("--------");
                foreach (var unmatchedMember in result.UnmatchedMembers)
                {
                    sb.AppendLine(unmatchedMember.ToString());
                }
                
                if (result.UnmatchedMembers.Length < 5)
                {
                    await ReplyAsync(sb.ToString());
                    return;
                }

                var tempFile = new FileInfo(Path.GetTempFileName());
                await File.WriteAllTextAsync(tempFile.FullName, sb.ToString());
                var attachment = new FileAttachment(tempFile.FullName, "unmatched.txt");
                await Context.Channel.SendFileAsync(attachment);
            }

            public PersonnelModule(PolicyDispatcher policyDispatcher)
            {
                _policyDispatcher = policyDispatcher;
            }
        }
    }

}
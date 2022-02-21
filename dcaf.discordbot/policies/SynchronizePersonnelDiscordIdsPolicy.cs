using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Model;
using TetraPak.XP;
using TetraPak.XP.Logging;

namespace DCAF.DiscordBot.Policies
{
    public class SynchronizePersonnelDiscordIdsPolicy : Policy<SyncIdsResult>
    {
        public const string PolicyName = "Synchronize Personnel Discord IDs";
        
        readonly IPersonnel _personnel;
        readonly IDiscordGuild _discordGuild;
        
        public async Task<Outcome<SyncIdsResult>> ExecuteAsync()
        {
            // todo Add logging
            var membersWithNoId = _personnel.Where(i => i.Id == Member.MissingId).ToArray();
            var updatedMembers = new List<Member>();
            var unmatched = new List<Member>();
            foreach (var member in membersWithNoId)
            {
                var outcome = await tryGetMemberDiscordIdAsync(member);
                if (!outcome)
                {
                    Log.Warning($"{this} cannot resolve member '{member}'");
                    unmatched.Add(member);
                    continue;
                }

                member.Id = outcome.Value.ToString();
                updatedMembers.Add(member);
            }

            var updateOutcome = await _personnel.UpdateAsync(updatedMembers.ToArray());
            if (!updateOutcome)
                return Outcome<SyncIdsResult>.Fail(updateOutcome.Exception!);

            return Outcome<SyncIdsResult>.Success(new SyncIdsResult(unmatched, updatedMembers));
        }

        public override async Task<Outcome> ResetCacheAsync()
        {
            var tasks = new Task[]
            {
                _personnel.ResetAsync(),
                _discordGuild.ResetAsync()
            };
            await Task.WhenAll(tasks);
            foreach (var task in tasks)
            {
                if (task is not Task<Outcome> outcomeTask)
                    continue;
                
                if (!outcomeTask.Result)
                    return Outcome.Fail(outcomeTask.Result.Exception!);
            }
            return Outcome.Success();
        }

        async Task<Outcome<ulong>> tryGetMemberDiscordIdAsync(Member member)
        {
            var outcome = await _discordGuild.GetUserWithNameAsync(member.DiscordName);
            return outcome
                ? Outcome<ulong>.Success(outcome.Value!.Id)
                : Outcome<ulong>.Fail(outcome.Exception!);
        }

        public SynchronizePersonnelDiscordIdsPolicy(
            IPersonnel personnel, 
            IDiscordGuild discordGuild,
            PolicyDispatcher dispatcher,
            ILog? log)
        : base(PolicyName, dispatcher, log)
        {
            _personnel = personnel;
            _discordGuild = discordGuild;
        }
    }
    
    public class SyncIdsResult : PolicyResult
    {
        public Member[] UnmatchedMembers { get;  }

        public Member[] UpdatedMembers { get; set; }

        public SyncIdsResult(List<Member> unmatched, List<Member> updated)
        {
            UnmatchedMembers = unmatched.ToArray();
            UpdatedMembers = updated.ToArray();
        }
    }
}
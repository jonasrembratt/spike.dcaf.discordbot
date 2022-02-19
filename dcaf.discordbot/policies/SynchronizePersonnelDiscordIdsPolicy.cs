using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Model;
using Discord.Commands;
using TetraPak.XP.Logging;

namespace DCAF.DiscordBot.Policies
{
    public class SynchronizePersonnelDiscordIdsPolicy : Policy
    {
        const string PolicyName = "Synchronize Personnel Discord IDs";
        
        // readonly EventCollection _events;
        readonly IPersonnel _personnel;
        readonly IDiscordGuild _discordGuild;
        
        public override async Task<Outcome> ExecuteAsync(PolicyArgs e)
        {
            // todo Add logging
            var membersWithNoId = _personnel.Where(i => i.Id == Member.MissingId).ToArray();
            var updatedMembers = new List<Member>();
            foreach (var member in membersWithNoId)
            {
                var outcome = await tryGetMemberDiscordIdAsync(member);
                if (!outcome)
                {
                    Log.Warning($"{this} cannot resolve member '{member}'");
                    continue;
                }

                member.Id = outcome.Value.ToString();
                updatedMembers.Add(member);
            }

            return await _personnel.Update(updatedMembers.ToArray());
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
}
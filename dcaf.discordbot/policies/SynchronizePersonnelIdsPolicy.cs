using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Model;
using Discord.WebSocket;
using TetraPak.XP;
using TetraPak.XP.Logging;

namespace DCAF.DiscordBot.Policies
{
    public class SynchronizePersonnelIdsPolicy : Policy<SyncIdsResult>
    {
        public const string PolicyName = "Synchronize Personnel Discord IDs";
        
        readonly IPersonnel _personnel;
        readonly IDiscordGuild _discordGuild;
        
        public async Task<Outcome<SyncIdsResult>> ExecuteAsync()
        {
            // todo Add logging
            var membersWithNoId = _personnel.Where(i => i.Id == Member.MissingId).ToArray();
            var updatedMembers = new List<Member>();
            var unmatchedMembers = new List<UnmatchedMember>();
            foreach (var member in membersWithNoId)
            {
                var outcome = await tryGetMemberDiscordIdAsync(member);
                if (!outcome)
                {
                    var potentialOutcome = await tryGetPotentialUsers(member);
                    Log.Warning($"{this} cannot resolve member '{member}'");
                    unmatchedMembers.Add(potentialOutcome
                        ? new UnmatchedMember(member, potentialOutcome.Value!.PotentialDiscordUsers)
                        : new UnmatchedMember(member, Array.Empty<SocketGuildUser>()));
                    continue;
                }

                member.Id = outcome.Value.ToString();
                updatedMembers.Add(member);
            }

            var updateOutcome = await _personnel.UpdateAsync(updatedMembers.ToArray());
            if (!updateOutcome)
                return Outcome<SyncIdsResult>.Fail(updateOutcome.Exception!);

            return Outcome<SyncIdsResult>.Success(new SyncIdsResult(unmatchedMembers, updatedMembers));
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
            var outcome = await _discordGuild.GetUserWithDiscordNameAsync(member.DiscordName);
            return outcome
                ? Outcome<ulong>.Success(outcome.Value!.Id)
                : Outcome<ulong>.Fail(outcome.Exception!);
        }
        
        async Task<Outcome<UnmatchedMember>> tryGetPotentialUsers(Member member)
        {
            var outcome = await _discordGuild.GetUserWithNicknameAsync(member.Forename, member.Surname);
            return outcome
                ? Outcome<UnmatchedMember>.Success(new UnmatchedMember(member, outcome.Value!))
                : Outcome<UnmatchedMember>.Fail(outcome.Exception!);
        }

        public SynchronizePersonnelIdsPolicy(
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
        public UnmatchedMember[] UnmatchedMembers { get;  }

        public Member[] UpdatedMembers { get; set; }

        public SyncIdsResult(List<UnmatchedMember> unmatched, List<Member> updated)
        {
            UnmatchedMembers = unmatched.ToArray();
            UpdatedMembers = updated.ToArray();
        }
    }

    public class UnmatchedMember
    {
        public Member Member { get; }

        public SocketGuildUser[] PotentialDiscordUsers { get; }

        public bool HasPotentialMatches => PotentialDiscordUsers.Any();

        public UnmatchedMember(Member member, SocketGuildUser[] potentialDiscordUsers)
        {
            Member = member;
            PotentialDiscordUsers = potentialDiscordUsers;
        }
    }
}
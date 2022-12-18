using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCAF.Model;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TetraPak.XP;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Policies
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class MemberSyncIdsPolicy : Policy
    {
        const string PolicyName = "member sync-ids";
        
        readonly IPersonnel<DiscordMember> _personnel;
        readonly IDiscordGuild _discordGuild;

        public override async Task<Outcome> ExecuteAsync(IConfiguration? config)
        {
            if (DateTimeSource.Current is XpDateTime { TimeAcceleration: > 20 })
                return Outcome.Fail("Ignores running the policy when time is accelerated more than 20 times");

            // todo Add logging
            var membersWithNoId = _personnel.Where(i => i.Id == Member.MissingId).ToArray();
            var updatedMembers = new List<DiscordMember>();
            var unmatchedMembers = new List<UnmatchedMember>();
            foreach (var member in membersWithNoId)
            {
                var outcome = await tryGetMemberDiscordIdAsync(member);
                if (!outcome)
                {
                    var potentialOutcome = await tryGetPotentialUsers(member);
                    Log.Warning($"{this} cannot resolve member '{member.Forename} {member.Surname} (Discord name: '{member.DiscordName}')'");
                    unmatchedMembers.Add(potentialOutcome
                        ? new UnmatchedMember(member, potentialOutcome.Value!.PotentialDiscordUsers)
                        : new UnmatchedMember(member, Array.Empty<SocketGuildUser>()));
                    continue;
                }

                member.Id = outcome.Value.ToString();
                updatedMembers.Add(member);
            }

            var updateOutcome = await _personnel.UpdateAsync(updatedMembers.ToArray());
            return updateOutcome 
                ? Outcome<SyncIdsResult>.Success(new SyncIdsResult(unmatchedMembers, updatedMembers)) 
                : Outcome<SyncIdsResult>.Fail(updateOutcome.Exception!);
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

        async Task<Outcome<ulong>> tryGetMemberDiscordIdAsync(DiscordMember member)
        {
            var outcome = await _discordGuild.GetUserWithDiscordNameAsync(member.DiscordName);
            return outcome
                ? Outcome<ulong>.Success(outcome.Value!.Id)
                : Outcome<ulong>.Fail(outcome.Exception!);
        }
        
        async Task<Outcome<UnmatchedMember>> tryGetPotentialUsers(DiscordMember member)
        {
            var outcome = await _discordGuild.GetUserWithNicknameAsync(member.Forename, member.Surname);
            return outcome
                ? Outcome<UnmatchedMember>.Success(new UnmatchedMember(member, outcome.Value!))
                : Outcome<UnmatchedMember>.Fail(outcome.Exception!);
        }

        public MemberSyncIdsPolicy(
            IPersonnel<DiscordMember> personnel, 
            IDiscordGuild discordGuild,
            PolicyDispatcher dispatcher,
            ILog? log)
        : base(PolicyName, dispatcher, log)
            {
            _personnel = personnel;
            _discordGuild = discordGuild;
        }
    }
    
    public sealed class SyncIdsResult : PolicyResult
    {
        public UnmatchedMember[] UnmatchedMembers { get;  }

        public DiscordMember[] UpdatedMembers { get; set; }

        public SyncIdsResult(List<UnmatchedMember> unmatched, List<DiscordMember> updated)
        {
            UnmatchedMembers = unmatched.ToArray();
            UpdatedMembers = updated.ToArray();
        }
    }

    public sealed class UnmatchedMember
    {
        public DiscordMember Member { get; }

        public override string ToString() => $"{Member} ({Member.DiscordName})" ;

        public SocketGuildUser[] PotentialDiscordUsers { get; }

        public bool HasPotentialMatches => PotentialDiscordUsers.Any();

        public UnmatchedMember(DiscordMember member, SocketGuildUser[] potentialDiscordUsers)
        {
            Member = member;
            PotentialDiscordUsers = potentialDiscordUsers;
        }
    }
}
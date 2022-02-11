using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Model;

namespace DCAF.DiscordBot.Policies
{
    public class UpdateMemberIdsPolicy : Policy
    {
        const string PolicyIdent = "sync-ids";
        
        // readonly EventCollection _events;
        readonly IPersonnel _personnel;
        readonly DiscordGuild _discordGuild;

        public override async Task<Outcome> ExecuteAsync()
        {
            // todo Add logging
            var membersWithNoId = _personnel.Where(i => i.Id == Member.MissingId).ToArray();
            var updatedMembers = new List<Member>();
            foreach (var member in membersWithNoId)
            {
                var outcome = await tryGetMemberDiscordIdAsync(member);
                if (!outcome)
                {
                    // todo log this as a warning
                    continue;
                }

                member.Id = outcome.Value.ToString();
                updatedMembers.Add(member);
            }

            return await _personnel.Update(updatedMembers.ToArray());
        }

        async Task<Outcome<ulong>> tryGetMemberDiscordIdAsync(Member member)
        {
            var outcome = await _discordGuild.GetDiscordUserWithName(member.DiscordName!);
            return outcome
                ? Outcome<ulong>.Success(outcome.Value!.Id)
                : Outcome<ulong>.Fail(outcome.Exception!);
        }

        public UpdateMemberIdsPolicy(
            IPersonnel personnel, 
            DiscordGuild discordGuild)
        : base(PolicyIdent)
        {
            _personnel = personnel;
            _discordGuild = discordGuild;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCAF._lib;
using DCAF.Model;
using Discord.Commands;
using TetraPak.XP;
using TetraPak.XP.Logging;

namespace DCAF.Discord.Policies
{
    public class AwolPolicy : Policy<SetAwolResult>
    {
        readonly IPersonnel<DiscordMember> _personnel;
        readonly GuildEventsRepository _events;

        public TimeSpan MaxInactiveTime { get; set; } = TimeSpan.FromDays(10);

        public async Task<Outcome<SetAwolResult>> ExecuteAsync(SetAwolArgs e)
        {
            var resetPersonnelTask = _personnel.ResetAsync();
            var allowedOutcome = e.GetAllowed();
            if (!allowedOutcome)
                return Outcome<SetAwolResult>.Fail(allowedOutcome.Exception!);

            var rsvpOutcome = e.GetRsvp();
            if (!rsvpOutcome)
                return Outcome<SetAwolResult>.Fail(allowedOutcome.Exception!);

            var timeFrame = new TimeFrame(
                DateTime.UtcNow.Subtract(allowedOutcome.Value), 
                DateTime.UtcNow.Subtract(rsvpOutcome.Value));

            var eventsOutcome = await _events.ReadEventsAsync(timeFrame);
            if (!eventsOutcome)
                return Outcome<SetAwolResult>.Fail($"Failed when reading events. {eventsOutcome.Message}");
            var events = eventsOutcome.Value!.ToArray();
            
            if (!events.Any())
                return Outcome<SetAwolResult>.Success(
                    new SetAwolResult("No members where found to be AWOL at this time"));
            
            await resetPersonnelTask;
            var membersOutcome = await _personnel.GetAllMembers();
            if (!membersOutcome)
                return Outcome<SetAwolResult>.Fail($"Failed when reading events. {membersOutcome.Message}");

            var awolMembersList = membersOutcome.Value!.ToList();
            var reactivateMembers = new List<DiscordMember>();

            
            var members = awolMembersList.ToArray();
            for (var i = 0; i < members.Length; i++)
            {
                var member = members[i];
                if (!member.IsIdentifiable)
                {
                    awolMembersList.Remove(member);
                    continue;
                }

                switch (member.Status)
                {
                    case MemberStatus.AWOL:
                        awolMembersList.Remove(member);
                        if (hasAnsweredRollCall(member))
                        {
                            member.Status = MemberStatus.Active;
                            reactivateMembers.Add(member);
                        }

                        break;
                    
                    case MemberStatus.Active when !hasAnsweredRollCall(member):
                        member.Status = MemberStatus.AWOL;
                        break;
                    
                    default:
                        awolMembersList.Remove(member);
                        break;
                }
            }

            var sbMessage = new StringBuilder();
            if (!awolMembersList.Any())
            {
                sbMessage.AppendLine("- No new members where found to be AWOL at this time");
            }

            if (!reactivateMembers.Any())
            {
                sbMessage.AppendLine("- No AWOL members where reactivated");
            }

            if (!awolMembersList.Any() && !reactivateMembers.Any())
                return Outcome<SetAwolResult>.Success(new SetAwolResult(sbMessage.ToString()));

            var updateOutcome = awolMembersList.Any() ? await _personnel.UpdateAsync(awolMembersList.ToArray()) : Outcome.Success();
            if (!updateOutcome)
                return Outcome<SetAwolResult>.Fail(
                    new Exception(
                        $"Failed when updating AWOL members in Sheet. {updateOutcome.Message}", 
                        updateOutcome.Exception!));

            updateOutcome = await _personnel.UpdateAsync(reactivateMembers.ToArray());
            if (!updateOutcome)
                return Outcome<SetAwolResult>.Fail(
                    new Exception(
                        $"Failed when updating AWOL members back to Active in Sheet. {updateOutcome.Message}", 
                        updateOutcome.Exception!));

            if (awolMembersList.Any())
            {
                sbMessage.AppendLine(
                    $"{awolMembersList.Count} members hasn't responded to event invitations between {timeFrame} and was set to 'AWOL'");
            }

            if (reactivateMembers.Any())
            {
                sbMessage.AppendLine(
                    $"{reactivateMembers.Count} members that was previously AWOL was reactivated");
            }
            return Outcome<SetAwolResult>.Success(
                new SetAwolResult(sbMessage.ToString())
                {
                    AwolMembers = awolMembersList.ToArray(),
                    ReactivatedMembers = reactivateMembers.ToArray()
                });

            bool hasAnsweredRollCall(Member member)
            {
                return events.Any(evt => evt.Signups.Any(su => su.DiscordId.ToString() == member.Id));
            }
        }

        public override Task<Outcome> ResetCacheAsync()
        {
            return Task.FromResult(Outcome.Fail(new Exception("!POLICY IS NOT IMPLEMENTED YET!")));
        }

        public AwolPolicy(GuildEventsRepository events, IPersonnel<DiscordMember> personnel, PolicyDispatcher dispatcher, ILog? log)
        : base("awol", dispatcher, log)
        { 
            _events = events;
            _personnel = personnel;
        }
    }

    public class SetAwolResult : PolicyResult
    {
        public DiscordMember[] AwolMembers { get; set; }

        public DiscordMember[] ReactivatedMembers { get; set; }
        
        public SetAwolResult(string message) : base(message)
        {
            AwolMembers = Array.Empty<DiscordMember>();
            ReactivatedMembers = Array.Empty<DiscordMember>();
        }
    }

    [NamedArgumentType]
    public class SetAwolArgs 
    {
        static readonly TimeSpan s_defaultAllowed = TimeSpan.FromDays(30);
        static readonly TimeSpan s_defaultRsvp = TimeSpan.FromDays(2);

        public string? Allowed { get; set; }
        
        public string? Rsvp { get; set; }
        
        public static SetAwolArgs Default => new() { Allowed = "30d", Rsvp = "48h" };

        internal Outcome<TimeSpan> GetAllowed()
        {
            if (string.IsNullOrWhiteSpace(Allowed))
                return Outcome<TimeSpan>.Success(s_defaultAllowed);

            return Allowed!.TryParseTimeSpan(DateTimeHelper.Units.Days, out var ts)
                ? Outcome<TimeSpan>.Success(ts)
                : Outcome<TimeSpan>.Fail($"Invalid '{nameof(Allowed)}' value: {Allowed}");
        }

        internal Outcome<TimeSpan> GetRsvp()
        {
            if (string.IsNullOrWhiteSpace(Rsvp))
                return Outcome<TimeSpan>.Success(s_defaultRsvp);

            return Rsvp!.TryParseTimeSpan(DateTimeHelper.Units.Hours, out var ts)
                ? Outcome<TimeSpan>.Success(ts)
                : Outcome<TimeSpan>.Fail($"Invalid '{nameof(Rsvp)}' value: {Rsvp}");
        }
    }
}
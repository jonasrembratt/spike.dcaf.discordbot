using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCAF.Model;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Policies
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class MemberStatusPolicy : Policy
    {
        const string PolicyName = "member status";
        
        readonly IPersonnel<DiscordMember> _personnel;
        readonly GuildEventsRepository _events;
        readonly MemberStatusPolicyArgs _conf;

        public override async Task<Outcome> ExecuteAsync(IConfiguration? config)
        {
            if (DateTimeSource.Current is XpDateTime { TimeAcceleration: > 20 })
                return Outcome.Fail("Ignores running the policy when time is accelerated more than 20 times");
            
            var resetPersonnelTask = _personnel.ResetAsync();
            var maxAllowedAbsence = _conf.PolicyConfigurations.Max(pc => pc.AllowedAbsence);
            var minRsvpTimeSpan = _conf.PolicyConfigurations.Min(pc => pc.RsvpTimeSpan);

            var timeFrame = new TimeFrame(
                DateTime.UtcNow.Subtract(maxAllowedAbsence), 
                DateTime.UtcNow.Subtract(minRsvpTimeSpan));

            var eventsOutcome = await _events.ReadEventsAsync(timeFrame);
            if (!eventsOutcome)
                return Outcome<SetMemberStatusResult>.Fail($"Failed when reading events. {eventsOutcome.Message}");
            
            var events = eventsOutcome.Value!.ToArray();
            
            const string Message = "Member roll-calls have been analysed"; 
            if (!events.Any())
                return Outcome<SetMemberStatusResult>.Success(
                    new SetMemberStatusResult($"{Message}. No changes where made at this time", null));
            
            var resetOutcome = await resetPersonnelTask;
            if (!resetOutcome)
            {
                Log.Error(resetOutcome.Exception!);
                return resetOutcome;
            }
            
            var membersOutcome = await _personnel.GetAllMembers();
            if (!membersOutcome)
                return Outcome<SetMemberStatusResult>.Fail($"Failed when reading events. {membersOutcome.Message}");

            var awolMembersList = membersOutcome.Value!.ToList();
            var reactivateMembers = new List<DiscordMember>();
            
            var members = awolMembersList.ToArray();
            var countSetToAwol = 0;
            var countResetToActive = 0;
            for (var i = 0; i < members.Length; i++)
            {
                var member = members[i];
                if (!member.IsIdentifiable)
                {
                    awolMembersList.Remove(member);
                    continue;
                }

                var rulesOutcome = getRules(member);
                if (!rulesOutcome)
                    continue; // todo maybe warn that no configured rules apply to this member?

                var rules = rulesOutcome.Value!;
                switch (member.Status)
                {
                    case MemberStatus.AWOL:
                        awolMembersList.Remove(member);
                        if (hasAnsweredRollCall(member, rules))
                        {
                            member.Status = MemberStatus.Active;
                            reactivateMembers.Add(member);
                        }

                        break;
                    
                    case MemberStatus.Active when !hasAnsweredRollCall(member, rules):
                        member.Status = MemberStatus.AWOL;
                        break;
                    
                    default:
                        awolMembersList.Remove(member);
                        break;
                }
            }

            if (!awolMembersList.Any() && !reactivateMembers.Any())
                return Outcome<SetMemberStatusResult>.Success(
                    new SetMemberStatusResult("Member roll-calls have been analysed and no member's status needed change", null)
                    {
                        AwolMembers = awolMembersList.ToArray(),
                        ReactivatedMembers = reactivateMembers.ToArray()
                    });

            var sbDetailedMessage = new StringBuilder();
            // sbMessage.AppendLine("Member roll-calls have been analysed");
            if (!awolMembersList.Any())
            {
                sbDetailedMessage.AppendLine("No members were marked 'AWOL'");
            }
            else
            {
                sbDetailedMessage.AppendLine("The following members were marked 'AWOL':");
                sbDetailedMessage.AppendLine("--------");
                foreach (var member in awolMembersList)
                {
                    var outcome = await _personnel.UpdateAsync(member);
                    sbDetailedMessage.AppendLine(
                        outcome 
                            ? $"  - {member}"
                            : $"  - Failed when setting {member} status. {outcome.Message}");
                    countSetToAwol += outcome ? 1 : 0;
                }
                sbDetailedMessage.AppendLine("--------");
            }

            if (!reactivateMembers.Any())
            {
                sbDetailedMessage.AppendLine("No AWOL members were reactivated");
            }
            else
            {
                sbDetailedMessage.AppendLine("The following members were reactivated:");
                sbDetailedMessage.AppendLine("--------");
                foreach (var member in reactivateMembers)
                {
                    var outcome = await _personnel.UpdateAsync(member);
                    sbDetailedMessage.AppendLine(
                        outcome 
                            ? $"  - {member}"
                            : $"  - Failed when setting {member} status. {outcome.Message}");
                    countResetToActive += outcome ? 1 : 0;
                }
                sbDetailedMessage.AppendLine("--------");
            }

            return Outcome<SetMemberStatusResult>.Success(
                new SetMemberStatusResult(Message, sbDetailedMessage.ToString())
                {
                    AwolMembers = awolMembersList.ToArray(),
                    ReactivatedMembers = reactivateMembers.ToArray()
                });

            bool hasAnsweredRollCall(Member member, MemberStatusPolicyRules rules)
            {
                var targetDate = XpDateTime.UtcNow.Subtract(rules.RsvpTimeSpan).Subtract(rules.AllowedAbsence);
                if (isExemptedNewMember(member, targetDate))
                {
                    Log.Debug($"{member} is a new member, exempt from the AWOL rules (joined {member.DateOfApplication.ToUniversalTime():s}Z)");
                    return true;
                }
                
                return events.Any(evt 
                    => evt.Date >= targetDate && evt.Signups.Any(su => su.DiscordId.ToString() == member.Id));
            }

            bool isExemptedNewMember(Member member, DateTime targetDate)
            {
                var newMemberForTimeSpan = DateTime.Today.Subtract(member.DateOfApplication);
                return DateTime.Today.Subtract(targetDate) > newMemberForTimeSpan;
            }
        }

        Outcome<MemberStatusPolicyRules> getRules(Member member)
        {
            // todo This is just (hard coded) placeholder code until scripting can resolve the correct policy rules
            //      It assumes there are just two types policies; one for member candidates (member.Grade == OFC) and all others, like in this configuration:
            /*
                "Candidates": {
                    "Criteria": "Member.Grade == OFC",
                    "SetStatus": "AWOL",
                    "AllowedAbsence": "30d",
                    "RsvpTimeSpan": "48h"
                },
                "Graduated": {
                    "Criteria": "Member.Grade < 'OF-1, OF-2, OF-3, OF-4, OF-5, OF-6'",
                    "SetStatus": "AWOL",
                    "AllowedAbsence": "30d",
                    "RsvpTimeSpan": "14d"
                }
             */
            return Outcome<MemberStatusPolicyRules>.Success(member.Grade == MemberGrade.OFC 
                ? _conf.PolicyConfigurations[0] 
                : _conf.PolicyConfigurations[1]);
        }

        public override Task<Outcome> ResetCacheAsync()
        {
            return Task.FromResult(Outcome.Fail(new Exception("!POLICY IS NOT IMPLEMENTED YET!")));
        }

        public MemberStatusPolicy(
            GuildEventsRepository events, 
            IPersonnel<DiscordMember> personnel, 
            PolicyDispatcher dispatcher, 
            ILog? log)
        : base(PolicyName, dispatcher, log)
        {
            _events = events;
            _personnel = personnel;
            _conf = new MemberStatusPolicyArgs(
                ConfigurationSectionDecoratorArgs.ForSubSection($"DCAF:Policies:{PolicyName}"));
        }
    }

    public sealed class SetMemberStatusResult : PolicyResult
    {
        public string? DetailedMessage { get; }

        public DiscordMember[] AwolMembers { get; set; }

        public DiscordMember[] ReactivatedMembers { get; set; }
        
        public SetMemberStatusResult(string message, string? detailedMessage) : base(message)
        {
            DetailedMessage = detailedMessage;
            AwolMembers = Array.Empty<DiscordMember>();
            ReactivatedMembers = Array.Empty<DiscordMember>();
        }
    }

    [NamedArgumentType]
    public sealed class SetAwolArgs : ConfigurationSectionDecorator
    {
        static readonly TimeSpan s_defaultAllowed = TimeSpan.FromDays(30);
        static readonly TimeSpan s_defaultRsvp = TimeSpan.FromDays(2);

        public string Criteria { get; set; }
        
        public string? AllowedAbsence { get; set; }
        
        public string? Rsvp { get; set; }
        
        public static SetAwolArgs Default => new() { AllowedAbsence = "30d", Rsvp = "48h" };
    
        internal Outcome<TimeSpan> GetAllowed()
        {
            if (string.IsNullOrWhiteSpace(AllowedAbsence))
                return Outcome<TimeSpan>.Success(s_defaultAllowed);
    
            return AllowedAbsence!.TryParseTimeSpan(TimeUnits.Days, out var ts, ignoreCase:true)
                ? Outcome<TimeSpan>.Success(ts)
                : Outcome<TimeSpan>.Fail($"Invalid '{nameof(AllowedAbsence)}' value: {AllowedAbsence}");
        }
    
        internal Outcome<TimeSpan> GetRsvp()
        {
            if (string.IsNullOrWhiteSpace(Rsvp))
                return Outcome<TimeSpan>.Success(s_defaultRsvp);
    
            return Rsvp!.TryParseTimeSpan(TimeUnits.Hours, out var ts, ignoreCase:true)
                ? Outcome<TimeSpan>.Success(ts)
                : Outcome<TimeSpan>.Fail($"Invalid '{nameof(Rsvp)}' value: {Rsvp}");
        }
    }
}
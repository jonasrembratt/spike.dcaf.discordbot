using System;
using System.Linq;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using DCAF.DiscordBot.Model;
using Discord.Commands;
using TetraPak.XP;
using TetraPak.XP.Logging;

namespace DCAF.DiscordBot.Policies
{
    public class SetAwolPolicy : Policy<SetAwolResult>
    {
        readonly IPersonnel _personnel;
        readonly GuildEventsRepository _events;

        public TimeSpan MaxInactiveTime { get; set; } = TimeSpan.FromDays(10);

        public async Task<Outcome<SetAwolResult>> ExecuteAsync(SetAwolArgs e)
        {
            var allowedOutcome = e.GetAllowed();
            if (!allowedOutcome)
                return Outcome<SetAwolResult>.Fail(allowedOutcome.Exception!);

            var rsvpOutcome = e.GetRsvp();
            if (!rsvpOutcome)
                return Outcome<SetAwolResult>.Fail(allowedOutcome.Exception!);

            var timeFrame = new TimeFrame(DateTime.UtcNow.Subtract(allowedOutcome.Value!), DateTime.UtcNow);
            var membersOutcome = await _personnel.GetAllMembers();
            if (!membersOutcome)
                return Outcome<SetAwolResult>.Fail($"Failed when reading events. {membersOutcome.Message}");
                
            var eventsOutcome = await _events.ReadEventsAsync(timeFrame);
            if (!eventsOutcome)
                return Outcome<SetAwolResult>.Fail($"Failed when reading events. {eventsOutcome.Message}");

            var awolMembers = membersOutcome.Value!.ToList();
            var events = eventsOutcome.Value!.ToArray();
            if (!events.Any())
                return Outcome<SetAwolResult>.Success(
                    new SetAwolResult("No members where found to be AWOL at this time"));
            
            var members = awolMembers.ToArray();
            for (var i = 0; i < members.Length; i++)
            {
                var member = members[i];
                
                if (!member.IsIdentifiable || member.Status != MemberStatus.Active || events.Any(
                        e => e.Signups.Any(su => su.DiscordId.ToString() == member.Id)))
                {
                    awolMembers.Remove(member);
                    continue;
                }

                member.Status = MemberStatus.AWOL;
            }
            
            if (!awolMembers.Any())
                return Outcome<SetAwolResult>.Success(new SetAwolResult("No members where found to be AWOL at this time"));

            var updateOutcome = await _personnel.UpdateAsync(awolMembers.ToArray());
            if (!updateOutcome)
                return Outcome<SetAwolResult>.Fail(
                    new Exception(
                        $"Failed when updating AWOL members in Sheet. {updateOutcome.Message}", 
                        updateOutcome.Exception!));
            
            return Outcome<SetAwolResult>.Success(new SetAwolResult($"{awolMembers.Count} was set to 'AWOL'")
            {
                AwolMembers = awolMembers.ToArray()
            });
        }

        public override Task<Outcome> ResetCacheAsync()
        {
            return Task.FromResult(Outcome.Fail(new Exception("!POLICY IS NOT IMPLEMENTED YET!")));
        }

        public SetAwolPolicy(GuildEventsRepository events, IPersonnel personnel, PolicyDispatcher dispatcher, ILog? log)
        : base("awol", dispatcher, log)
        { 
            _events = events;
            _personnel = personnel;
        }
    }

    public class SetAwolResult : PolicyResult
    {
        public Member[] AwolMembers { get; set; }
        
        public SetAwolResult(string message) : base(message) => AwolMembers = Array.Empty<Member>();
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

            return Allowed.TryParseTimeSpan(DateTimeHelper.Units.Days, out var ts)
                ? Outcome<TimeSpan>.Success(ts)
                : Outcome<TimeSpan>.Fail($"Invalid '{nameof(Allowed)}' value: {Allowed}");
        }

        internal Outcome<TimeSpan> GetRsvp()
        {
            if (string.IsNullOrWhiteSpace(Rsvp))
                return Outcome<TimeSpan>.Success(s_defaultRsvp);

            return Rsvp.TryParseTimeSpan(DateTimeHelper.Units.Hours, out var ts)
                ? Outcome<TimeSpan>.Success(ts)
                : Outcome<TimeSpan>.Fail($"Invalid '{nameof(Rsvp)}' value: {Rsvp}");
        }
    }
}
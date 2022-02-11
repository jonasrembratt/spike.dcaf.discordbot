using System;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using DCAF.DiscordBot.Model;

namespace DCAF.DiscordBot.Policies
{
    public class SetMembersAwolPolicy : Policy
    {
        readonly IPersonnel _personnel;
        readonly EventCollection _events;

        public TimeSpan MaxInactiveTime { get; set; } = TimeSpan.FromDays(10);

        public override async Task<Outcome> ExecuteAsync()
        {
            return Outcome.Success();
            
            // work backwards, from last event to older ones ...

            // var eventsArray = _events.ToArray(); todo
            // for (var i = _events.Count(); i != 0; i--)
            // {
            //     var evt = eventsArray[i];
            //     IEnumerable<> getSilentMembers
            // }
            
        }

        public SetMembersAwolPolicy(EventCollection events, IPersonnel personnel)
        : base("apply-awol")
        {
            _events = events;
            _personnel = personnel;
        }
    }
}
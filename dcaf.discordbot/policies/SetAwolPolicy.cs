using System;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using DCAF.DiscordBot.Model;

namespace DCAF.DiscordBot.Policies
{
    public class SetAwolPolicy : Policy
    {
        readonly IPersonnel _personnel;
        readonly EventCollection _events;

        public TimeSpan MaxInactiveTime { get; set; } = TimeSpan.FromDays(10);

        public override async Task<Outcome> ExecuteAsync()
        {
            return Outcome.Fail(new Exception("!POLICY IS NOT IMPLEMENTED YET!"));
            // work backwards, from last event to older ones ...

            // var eventsArray = _events.ToArray(); todo
            // for (var i = _events.Count(); i != 0; i--)
            // {
            //     var evt = eventsArray[i];
            //     IEnumerable<> getSilentMembers
            // }
        }

        public override Task<Outcome> ResetCacheAsync()
        {
            return Task.FromResult(Outcome.Fail(new Exception("!POLICY IS NOT IMPLEMENTED YET!")));
        }

        public SetAwolPolicy(EventCollection events, IPersonnel personnel, PolicyDispatcher dispatcher)
        : base("apply-awol", dispatcher)
        {
            _events = events;
            _personnel = personnel;
        }
    }
}
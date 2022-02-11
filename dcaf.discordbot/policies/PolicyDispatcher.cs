using System.Collections.Generic;
using System.Linq;

namespace DCAF.DiscordBot.Policies
{
    public class PolicyDispatcher
    {
        readonly Dictionary<string, Policy> _policies;

        public bool TryGetPolicy(string name, out Policy policy) => _policies.TryGetValue(name, out policy);

        public PolicyDispatcher(params Policy[] policies)
        {
            _policies = policies.ToDictionary(i => i.Name);
        }
    }
}
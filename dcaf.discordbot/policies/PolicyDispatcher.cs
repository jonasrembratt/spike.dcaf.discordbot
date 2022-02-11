using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DCAF.DiscordBot.Policies
{
    public class PolicyDispatcher 
    {
        readonly Dictionary<string, Policy> _policies = new();

        public Policy[] GetPolicies() => _policies.Values.ToArray();

        internal bool ContainsPolicy(string name) => _policies.ContainsKey(name);

        public bool TryGetPolicy(string name, [NotNullWhen(true)] out Policy? policy) => _policies.TryGetValue(name, out policy);

        public void Add(Policy policy)
        {
            if (_policies.ContainsKey(policy.Name))
                throw new ArgumentException($"Policy was already added: {policy}", nameof(policy));
            
            _policies.Add(policy.Name, policy);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;

namespace DCAF.DiscordBot.Policies
{
    public class ResetPoliciesPolicy : Policy
    {
        public override async Task<Outcome> ExecuteAsync(string[] args)
        {
            var tasks = new List<Task<Outcome>>();
            var policies = Dispatcher.GetPolicies();
            foreach (var policy in policies)
            {
                if (policy == this)
                    continue;
                
                tasks.Add(policy.ResetCacheAsync());
            }

            await Task.WhenAll(tasks);
            var failed = tasks.Where(t => !t.Result).ToArray();
            if (!failed.Any())
                return Outcome.Success("All policies was reset");
            
            var sb = new StringBuilder();
            sb.AppendLine("One or more policy failed to reset:");
            foreach (var task in failed)
            {
                sb.AppendLine(task.Result.Message);
            }
            return Outcome.Fail(new Exception(sb.ToString()));
        }

        public override async Task<Outcome> ResetCacheAsync()
        {
            throw new NotSupportedException();
        }

        public ResetPoliciesPolicy(PolicyDispatcher dispatcher) 
        : base("reset-all", dispatcher)
        {
        }
    }
}
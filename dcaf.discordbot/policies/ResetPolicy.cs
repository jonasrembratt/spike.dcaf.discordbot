using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using TetraPak.XP.Logging;

namespace DCAF.DiscordBot.Policies
{
    public class ResetPolicy : Policy
    {
        const string IdentAll = "all";
        
        public override async Task<Outcome> ExecuteAsync(PolicyArgs e)
        {
            var args = e.Parameters;
            var policyName = args.Any() ? args[0] : IdentAll;
            var tasks = new List<Task<Outcome>>();
            Policy[] policies;
            if (policyName == IdentAll)
            {
                policies = Dispatcher.GetPolicies();
            }
            else if (Dispatcher.TryGetPolicy(policyName, out var selectedPolicy))
            {
                policies = new[] { selectedPolicy };
            }
            else
                return Outcome.Fail(new ArgumentOutOfRangeException($"Unknown policy: {policyName}"));


            // var policies = Dispatcher.GetPolicies();
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

        public override Task<Outcome> ResetCacheAsync()
        {
            throw new NotSupportedException();
        }

        public ResetPolicy(PolicyDispatcher dispatcher, ILog? log) 
        : base("reset", dispatcher, log)
        {
        }
    }
}
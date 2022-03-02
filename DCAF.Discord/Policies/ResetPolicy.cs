using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetraPak.XP;
using TetraPak.XP.Logging;

namespace DCAF.Discord.Policies
{
    public class ResetPolicy : Policy<PolicyResult>
    {
        const string IdentAll = "all";
        
        public async Task<Outcome<PolicyResult>> ExecuteAsync(PolicyArgs e)
        {
            var args = e.Parameters;
            var policyName = args.Any() ? args[0] : IdentAll;
            var tasks = new List<Task<Outcome>>();
            IPolicy[] policies;
            if (policyName == IdentAll)
            {
                policies = Dispatcher.GetPolicies();
            }
            else if (Dispatcher.TryGetPolicy(policyName, out var selectedPolicy))
            {
                policies = new[] { selectedPolicy };
            }
            else
                return Outcome<PolicyResult>.Fail(new ArgumentOutOfRangeException($"Unknown policy: {policyName}"));


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
                return Outcome<PolicyResult>.Success(new PolicyResult("All policies was reset"));
            
            var sb = new StringBuilder();
            sb.AppendLine("One or more policy failed to reset:");
            foreach (var task in failed)
            {
                sb.AppendLine(task.Result.Message);
            }
            return Outcome<PolicyResult>.Fail(new Exception(sb.ToString()));
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
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TetraPak.XP;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Policies.cleanupMemberApplications;

public sealed class MemberAppsCleanupPolicy : Policy
{
    public MemberAppsCleanupPolicy(string name, PolicyDispatcher dispatcher, ILog? log) : base(name, dispatcher, log)
    {
    }

    public override Task<Outcome> ExecuteAsync(IConfiguration? config)
    {
        throw new System.NotImplementedException();
    }

    public override Task<Outcome> ResetCacheAsync()
    {
        throw new System.NotImplementedException();
    }

    // public MemberAppsCleanupPolicy(GoogleMemberApplicationSheet)
    // {
    //     
    // }
}
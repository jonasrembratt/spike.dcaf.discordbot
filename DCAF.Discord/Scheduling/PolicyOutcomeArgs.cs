using System;
using System.Threading.Tasks;
using DCAF.Discord.Policies;
using Discord;
using Discord.WebSocket;
using TetraPak.XP;

namespace DCAF.Discord.Scheduling;

public sealed class PolicyOutcomeArgs : EventArgs
{
    public IPolicy Policy { get; }

    public Outcome Outcome { get; }

    ISocketMessageChannel OutputChannel { get; }

    public DateTime? NextRunTime { get;  }

    public Task SendMessageAsync(string message)
    {
        return DateTimeSource.Current.IsTimeSkewed 
            ? Task.CompletedTask 
            : OutputChannel.SendMessageAsync(message);
    }

    public Task SendFileAsync(FileAttachment fileAttachment)
    {
        return DateTimeSource.Current.IsTimeSkewed
            ? Task.CompletedTask
            : OutputChannel.SendFileAsync(fileAttachment);
    }

    public PolicyOutcomeArgs(IPolicy policy, Outcome outcome, ISocketMessageChannel outputChannel, DateTime? nextRunTime)
    {
        Policy = policy;
        Outcome = outcome;
        OutputChannel = outputChannel;
        NextRunTime = nextRunTime;
    }
}
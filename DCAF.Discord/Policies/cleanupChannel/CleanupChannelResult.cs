using Discord.WebSocket;

namespace DCAF.Discord.Policies;

sealed class CleanupChannelResult : PolicyResult
{
    internal ISocketMessageChannel Channel { get; }

    public bool Silent { get; set; }
    
    public CleanupChannelResult(string message, ISocketMessageChannel channel, bool silent) 
        : base(message)
    {
        Channel = channel;
        Silent = silent;
    }
}
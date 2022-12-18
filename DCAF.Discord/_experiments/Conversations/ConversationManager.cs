using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DCAF.Discord.Conversations;

public sealed class ConversationManager
{
    readonly DiscordService _discordService;
    readonly Dictionary<ulong, Conversation> _conversations = new();

    public async Task StartConversationAsync(Conversation conversation)
    {
        await connectListenersAsync();
    }

    async Task connectListenersAsync()
    {
        var client = await _discordService.GetReadyClientAsync();
        client.ButtonExecuted += onButtonExecuted;
        client.SelectMenuExecuted += onMenuExecuted;
    }

    Task onMenuExecuted(SocketMessageComponent arg)
    {
        // todo
        return Task.CompletedTask;
    }

    Task onButtonExecuted(SocketMessageComponent arg)
    {
        throw new System.NotImplementedException();
    }

    public ConversationManager(DiscordService discordService)
    {
        _discordService = discordService;
    }
}
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using Discord.WebSocket;

namespace dcaf.discordbot.Discord
{
    public interface IDiscordGuild
    {
        DiscordSocketClient DiscordClient { get; }
        
        SocketGuild SocketGuild { get; }
        
        Task<Outcome<SocketGuildUser>> GetDiscordUserWithNameAsync(DiscordName discordName);
        
        Task<Outcome> ResetAsync();
    }
}
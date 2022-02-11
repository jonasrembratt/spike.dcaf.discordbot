using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using Discord.WebSocket;

namespace dcaf.discordbot.Discord
{
    public interface IDiscordGuild
    {
        Task<Outcome<SocketGuildUser>> GetDiscordUserWithName(DiscordName discordName);
        
        Task<Outcome> ResetAsync();
    }
}
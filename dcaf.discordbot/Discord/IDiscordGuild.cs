using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using Discord.WebSocket;

namespace dcaf.discordbot.Discord
{
    public interface IDiscordGuild
    {
        Task<SocketGuild> GetSocketGuildAsync();

        Task<Outcome<SocketGuildUser>> GetDiscordUserWithNameAsync(DiscordName discordName);
        
        Task<Outcome> ResetAsync();
    }
}
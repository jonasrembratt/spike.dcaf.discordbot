using System.Threading.Tasks;
using Discord.WebSocket;
using TetraPak.XP;

namespace dcaf.discordbot.Discord
{
    public interface IDiscordGuild
    {
        Task<SocketGuild> GetSocketGuildAsync();

        Task<Outcome<SocketGuildUser>> GetUserWithNameAsync(DiscordName discordName);
        
        Task<Outcome<SocketGuildUser>> GetUserAsync(ulong id);

        Task<Outcome> ResetAsync();
    }
}
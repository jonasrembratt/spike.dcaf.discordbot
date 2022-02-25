using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using TetraPak.XP;

namespace dcaf.discordbot.Discord
{
    public interface IDiscordGuild
    {
        Task<SocketGuild> GetSocketGuildAsync();

        Task<Outcome<SocketGuildUser>> GetUserWithDiscordNameAsync(DiscordName discordName);

        Task<Outcome<SocketGuildUser[]>> GetUserWithNicknameAsync(string forename, string? surname);

        Task<Outcome<SocketGuildUser>> GetUserAsync(ulong id);

        Task<Outcome> ResetAsync();
    }
}
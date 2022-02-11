using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using Discord.WebSocket;

namespace dcaf.discordbot.Discord
{
    public class DiscordGuild : IDiscordGuild
    {
        readonly SocketGuild _guild;
        readonly TaskCompletionSource<Outcome<SocketGuildUser[]>?> _loadUsersTcs = new();
        Outcome<SocketGuildUser[]>? _usersOutcome;
        Dictionary<DiscordName, SocketGuildUser>? _discordNameIndex;

        public async Task<Outcome<SocketGuildUser>> GetDiscordUserWithName(DiscordName discordName)
        {
            var outcome = await getUsersAsync();
            if (!outcome)
                return Outcome<SocketGuildUser>.Fail(
                    new ArgumentOutOfRangeException($"Cound not find discord user {discordName}. {outcome.Exception!.Message}"));

            return _discordNameIndex!.TryGetValue(discordName, out var user)
                ? Outcome<SocketGuildUser>.Success(user) 
                : Outcome<SocketGuildUser>.Fail( 
                    new ArgumentOutOfRangeException($"Could not find discord user {discordName}"));
        }

        void loadUsersAsync()
        {
            Task.Run(async () =>
            {
                try
                {
                    await _guild.DownloadUsersAsync();
                    var users = _guild.Users.ToArray();
                    _discordNameIndex = users.ToDictionary(i => new DiscordName(i.Username, i.Discriminator));
                    _loadUsersTcs.SetResult(_usersOutcome = Outcome<SocketGuildUser[]>.Success(users));
                }
                catch (Exception ex)
                {
                    _loadUsersTcs.SetResult(Outcome<SocketGuildUser[]>.Fail(ex));
                }
            });
        }

        Task<Outcome<SocketGuildUser[]>> getUsersAsync()
        {
            if (_usersOutcome is { })
                return Task.FromResult(_usersOutcome);
            
            _loadUsersTcs.AwaitResult();
            return Task.FromResult(_usersOutcome!);
        }

        public DiscordGuild(DiscordSocketClient discordClient, ulong guildId)
        {
            _guild = discordClient.GetGuild(guildId);
            loadUsersAsync();
        }
    }

    public interface IDiscordGuild
    {
        Task<Outcome<SocketGuildUser>> GetDiscordUserWithName(DiscordName discordName);
    }
}
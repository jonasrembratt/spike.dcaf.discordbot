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
        TaskCompletionSource<Outcome<SocketGuildUser[]>?> _loadUsersTcs = new();
        Outcome<SocketGuildUser[]>? _usersOutcome;
        Dictionary<DiscordName, SocketGuildUser>? _discordNameIndex;
        DateTime _lastReadUsers = DateTime.MinValue;

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

        public Task<Outcome> ResetAsync()
        {
            _loadUsersTcs = new TaskCompletionSource<Outcome<SocketGuildUser[]>?>();
            loadUsersAsync(true);
            _loadUsersTcs.AwaitResult();
            return Task.FromResult(Outcome.Success());
        }

        void loadUsersAsync(bool reload)
        {
            if ((_discordNameIndex?.Any() ?? false) && !reload)
            {
                _loadUsersTcs.SetResult(_usersOutcome);
                return;
            }

            if (DateTime.Now.Subtract(_lastReadUsers) < TimeSpan.FromSeconds(20))
            {
                Console.WriteLine($"@@@ Guild won't reset (was last reset: {_lastReadUsers:u})"); // nisse
                _loadUsersTcs.SetResult(_usersOutcome);
                return;
            }
            
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"@@@ Downloads users ..."); // nisse
                    await _guild.DownloadUsersAsync();
                    var users = _guild.Users.ToArray();
                    Console.WriteLine($"@@@ Downloads users - DONE!"); // nisse
                    _discordNameIndex = users.ToDictionary(i => new DiscordName(i.Username, i.Discriminator));
                    _loadUsersTcs.SetResult(_usersOutcome = Outcome<SocketGuildUser[]>.Success(users));
                    _lastReadUsers = DateTime.Now;
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
            _guild = discordClient.GetGuild(guildId) ?? throw new ArgumentOutOfRangeException($"Guild not found: {guildId}");
            loadUsersAsync(false);
        }
    }
}
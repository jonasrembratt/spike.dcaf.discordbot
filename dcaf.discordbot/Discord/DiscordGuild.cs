using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using Discord.WebSocket;
using TetraPak.XP.Configuration;

namespace dcaf.discordbot.Discord
{
    public class DiscordGuild : IDiscordGuild
    {
        TaskCompletionSource<Outcome<SocketGuildUser[]>?> _loadUsersTcs = new();
        Outcome<SocketGuildUser[]>? _usersOutcome;
        Dictionary<DiscordName, SocketGuildUser>? _discordNameIndex;
        DateTime _lastReadUsers = DateTime.MinValue;
        SocketGuild? _socketGuild;

        public DiscordService Discord { get; }

        public ulong GuildId { get; }

        public async Task<SocketGuild> GetSocketGuildAsync()
        {
            if (_socketGuild is { })
                return _socketGuild;
                
            var client = await Discord.GetReadyClientAsync();
            return _socketGuild = client.GetGuild(GuildId) ?? throw new ArgumentOutOfRangeException($"Guild not found: {GuildId}");
        }

        public async Task<Outcome<SocketGuildUser>> GetDiscordUserWithNameAsync(DiscordName discordName)
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
            Task.Run(async () =>
            {
                var client = await Discord.GetReadyClientAsync();
                var socketGuild = client.GetGuild(GuildId) ?? throw new ArgumentOutOfRangeException($"Guild not found: {GuildId}");

                if ((_discordNameIndex?.Any() ?? false) && !reload)
                {
                    _loadUsersTcs.SetResult(_usersOutcome);
                    return;
                }

                if (DateTime.Now.Subtract(_lastReadUsers) < TimeSpan.FromSeconds(20))
                {
                    _loadUsersTcs.SetResult(_usersOutcome);
                    return;
                }

                try
                {
                    await socketGuild.DownloadUsersAsync();
                    var users = socketGuild.Users.ToArray();
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

        public DiscordGuild(DiscordService discord, DcafConfiguration dcafConfig)
        {
            GuildId = dcafConfig.GuildId;
            if (GuildId == 0)
                throw new ConfigurationException($"Missing configuration: {nameof(DcafConfiguration.GuildId)}");

            Discord = discord;
            loadUsersAsync(false);
        }
    }
}
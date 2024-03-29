using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using TetraPak.XP;
using TetraPak.XP.Configuration;

namespace DCAF.Discord
{
    public class DiscordGuild : IDiscordGuild
    {
        TaskCompletionSource<Outcome<SocketGuildUser[]>> _loadUsersTcs = new();
        Outcome<SocketGuildUser[]>? _usersOutcome;
        Dictionary<DiscordName, SocketGuildUser>? _discordNameIndex;
        Dictionary<ulong, SocketGuildUser>? _userIdIndex;
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

        public async Task<Outcome<SocketGuildUser>> GetUserWithDiscordNameAsync(DiscordName discordName)
        {
            var outcome = await getUsersAsync();
            if (!outcome)
                return fail();

            if (_discordNameIndex!.TryGetValue(discordName, out var user))
                return Outcome<SocketGuildUser>.Success(user);

            if (discordName.Discriminator.IsAssigned())
                return fail();
            
            // seems the user hasn't provided the discriminator element of his/her Discord name; try looking for a user that shares the name element only ...
            var usersWithNameElement = outcome.Value!.Where(u => u.Username == discordName.Name).ToArray();
            if (usersWithNameElement.Length != 1)
                return fail();
            
            user = usersWithNameElement.First();
            return Outcome<SocketGuildUser>.Success(user);

            Outcome<SocketGuildUser> fail() => Outcome<SocketGuildUser>.Fail(
                new ArgumentOutOfRangeException($"Could not find discord user {discordName}"));
        }

        public async Task<Outcome<SocketGuildUser[]>> GetUserWithNicknameAsync(string forename, string? surname)
        {
            var outcome = await getUsersAsync();
            if (!outcome)
                return Outcome<SocketGuildUser[]>.Fail(
                    new ArgumentOutOfRangeException($"Cound not find discord user '{forename} {surname}'. {outcome.Message}"));

            var allUsers = outcome.Value!.Where(u => u.Nickname is { }).ToArray();
            var users = allUsers.Where(u =>
                u.Username.Equals($"{forename} {surname}", StringComparison.InvariantCultureIgnoreCase)).ToArray();
            
            return users.Any() 
                ? Outcome<SocketGuildUser[]>.Success(users) 
                : Outcome<SocketGuildUser[]>.Fail(new ArgumentOutOfRangeException($"No users found"));
        }

        public async Task<Outcome<SocketGuildUser>> GetUserAsync(ulong id)
        {
            var outcome = await getUsersAsync();
            if (!outcome)
                return Outcome<SocketGuildUser>.Fail(
                    new ArgumentOutOfRangeException(
                        $"Cound not find discord user with id {id.ToString()}. {outcome.Exception!.Message}"));
            
            return _userIdIndex!.TryGetValue(id, out var user)
                ? Outcome<SocketGuildUser>.Success(user) 
                : Outcome<SocketGuildUser>.Fail( 
                    new ArgumentOutOfRangeException($"Could not find discord user with id {id.ToString()}"));
        }

        public async Task<Outcome> ResetAsync()
        {
            _loadUsersTcs = new TaskCompletionSource<Outcome<SocketGuildUser[]>>();
            loadUsersAsync(true);
            var outcome = await _loadUsersTcs.GetOutcomeAsync();
            return outcome;
        }

        void loadUsersAsync(bool reload)
        {
            Task.Run(async () =>
            {
                var client = await Discord.GetReadyClientAsync();
                var socketGuild = client.GetGuild(GuildId) ?? throw new ArgumentOutOfRangeException($"Guild not found: {GuildId}");

                if ((_discordNameIndex?.Any() ?? false) && !reload)
                {
                    _loadUsersTcs.SetResult(_usersOutcome!);
                    return;
                }

                if (XpDateTime.Now.Subtract(_lastReadUsers) < TimeSpan.FromSeconds(20))
                {
                    _loadUsersTcs.SetResult(_usersOutcome!);
                    return;
                }

                try
                {
                    await socketGuild.DownloadUsersAsync();
                    var users = socketGuild.Users.ToArray();
                    _discordNameIndex = users.ToDictionary(user => new DiscordName(user.Username, user.Discriminator));
                    _userIdIndex = users.ToDictionary(user => user.Id);
                    _loadUsersTcs.SetResult(_usersOutcome = Outcome<SocketGuildUser[]>.Success(users));
                    _lastReadUsers = XpDateTime.Now;
                }
                catch (Exception ex)
                {
                    _loadUsersTcs.SetResult(Outcome<SocketGuildUser[]>.Fail(ex));
                }
            });
        }

        async Task<Outcome<SocketGuildUser[]>> getUsersAsync()
        {
            if (_usersOutcome is { })
                return _usersOutcome;
            
            _usersOutcome = await _loadUsersTcs.GetOutcomeAsync();
            return _usersOutcome;
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
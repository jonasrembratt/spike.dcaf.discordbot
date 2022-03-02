using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace DCAF.Discord.Commands
{
    public class CommandHandler 
    {
        readonly DiscordSocketClient _client;
        readonly CommandService _commands;
        IServiceProvider? _services;

        public async Task InstallCommandsAsync(IServiceProvider services)
        {
            _services = services;
            _client.MessageReceived += onCommandReceived;
            await _commands.AddModulesAsync(GetType().Assembly, services);
        }

        Task onCommandReceived(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message)
                return Task.CompletedTask;

            var argPos = 0;
            if (!message.HasCharPrefix('!', ref argPos) || 
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos) || 
                  message.Author.IsBot)
                return Task.CompletedTask;

            var context = new SocketCommandContext(_client, message);
            return Task.Run(() => 
                _commands.ExecuteAsync(context, argPos, _services!));
        }

        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _client = client;
            _commands = commands;
        }
    }
}
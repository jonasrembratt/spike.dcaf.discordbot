using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace DCAF.DiscordBot.Policies
{
    public class PolicyDispatcher 
    {
        readonly Dictionary<string, Policy> _policies = new();
        readonly CommandService _commandService;
        readonly DiscordSocketClient _client;

        public Policy[] GetPolicies() => _policies.Values.ToArray();

        internal bool ContainsPolicy(string name) => _policies.ContainsKey(name);

        public bool TryGetPolicy(string name, [NotNullWhen(true)] out Policy? policy) => _policies.TryGetValue(name, out policy);

        public void Add(Policy policy)
        {
            if (_policies.ContainsKey(policy.Name))
                throw new ArgumentException($"Policy was already added: {policy}", nameof(policy));
            
            _policies.Add(policy.Name, policy);
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += handleCommandAsync;
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        async Task handleCommandAsync(SocketMessage message)
        {
            if (message is not SocketUserMessage uMsg)
                return;

            var argPos = 0;
            if (!(uMsg.HasCharPrefix('!', ref argPos) 
                || uMsg.HasMentionPrefix(_client.CurrentUser, ref argPos))
                || uMsg.Author.IsBot)
                return;

            var context = new SocketCommandContext(_client, uMsg);
            await _commandService.ExecuteAsync(context, argPos, null);
        }

        public PolicyDispatcher(DiscordSocketClient client,  CommandService commandService)
        {
            _client = client;
            _commandService = commandService;
        }
    }
}
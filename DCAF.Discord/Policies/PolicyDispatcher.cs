#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace DCAF.Discord.Policies
{
    public sealed class PolicyDispatcher
    {
        readonly Dictionary<string, IPolicy> _policiesNameIndex = new();
        readonly Dictionary<Type, IPolicy> _policiesTypeIndex = new();
        readonly CommandService _commandService;
        readonly DiscordService _discord;

        public IPolicy[] GetPolicies() => _policiesTypeIndex.Values.ToArray();

        internal bool ContainsPolicy(string name) => _policiesNameIndex.ContainsKey(name);

        public bool TryGetPolicy(
            string name, 
#if NET5_0_OR_GREATER            
            [NotNullWhen(true)]
#endif 
            out IPolicy? policy) 
            => _policiesNameIndex.TryGetValue(name, out policy);


        public bool TryGetPolicy<T>(
#if NET5_0_OR_GREATER            
            [NotNullWhen(true)]
#endif 
            out T? policy) where T : IPolicy
        {
            if (_policiesTypeIndex.TryGetValue(typeof(T), out var iPolicy) && iPolicy is T tPolicy)
            {
                policy = tPolicy;
                return true;
            }

            policy = default;
            return false;
        }

        public void Add(IPolicy policy)
        {
            if (_policiesNameIndex.ContainsKey(policy.Name) || _policiesTypeIndex.ContainsKey(policy.GetType()))
                throw new ArgumentException($"Policy was already added: {policy}", nameof(policy));
            
            _policiesNameIndex.Add(policy.Name, policy);
            _policiesTypeIndex.Add(policy.GetType(), policy);
        }

        public async Task InstallCommandsAsync()
        {
            var client = await _discord.GetReadyClientAsync();
            client.MessageReceived += handleCommandAsync;
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        async Task handleCommandAsync(SocketMessage message)
        {
            if (message is not SocketUserMessage uMsg)
                return;

            var client = await _discord.GetReadyClientAsync();
            var argPos = 0;
            if (!(uMsg.HasCharPrefix('!', ref argPos) 
                || uMsg.HasMentionPrefix(client.CurrentUser, ref argPos))
                || uMsg.Author.IsBot)
                return;

            var context = new SocketCommandContext(client, uMsg);
            await _commandService.ExecuteAsync(context, argPos, null);
        }

        public PolicyDispatcher(DiscordService discord, CommandService commandService)
        {
            _discord = discord;
            _commandService = commandService;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DCAF.Discord.Policies;
using Discord.WebSocket;
using TetraPak.XP;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Scheduling
{
    public sealed class Scheduler
    {
        readonly DcafConfiguration _conf;
        readonly PolicyDispatcher _policyDispatcher;
        readonly IDiscordGuild _guild;
        ISocketMessageChannel? _outputChannel;
        readonly List<PolicyOutcomeHandler> _policyOutcomeHandlers = new();
        readonly ILog? _log;

        public Task<Outcome> RunAsync(CancellationTokenSource cts) 
            => Task.Run(() => run(cts.Token), cts.Token);

        async Task<Outcome> run(CancellationToken ct)
        {
            if (_conf.Scheduler.DiscordOutputChannel == 0)
                return Outcome.Fail($"No '{nameof(DcafConfiguration.Scheduler.DiscordOutputChannel)}' configuration");
            
            var guild = await _guild.GetSocketGuildAsync();
            _outputChannel =  guild.TextChannels.FirstOrDefault(i => i.Id == _conf.Scheduler.DiscordOutputChannel);
            if (_outputChannel is null)
                return Outcome.Fail($"Discord output channel not found: '{nameof(DcafConfiguration.Scheduler.DiscordOutputChannel)}'");

            if (!_conf.Scheduler.Actions.Any())
            {
                var ex = new Exception("Scheduler found no schedule entries");
                await outputAsync(ex.Message);
                return Outcome.Fail(ex);
            }

            var actions = new List<ScheduledAction>();
            foreach (var actionConfig in _conf.Scheduler.Actions)
            {
                if (!actionConfig.Enabled)
                    continue;
                
                var policies = new List<IPolicy>();
                foreach (var name in actionConfig.Policies)
                {
                    if (!_policyDispatcher.TryGetPolicy(name, out var policy))
                    {
                        var ex = new Exception($"Policy not found: '{name}' (in scheduled action '{actionConfig.Key}')");
                        await outputAsync(ex.Message);
                        return Outcome.Fail(ex);
                    }
                    policies.Add(policy!);
                }

                var action = new ScheduledAction(actionConfig);
                action.SetPolicies(policies);
                actions.Add(action);
            }

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    foreach (var action in actions)
                    {
#pragma warning disable CS4014
                        action.RunIfScheduled(_policyOutcomeHandlers.ToArray(), _outputChannel);
#pragma warning restore CS4014
                    }

                    await Task.Delay(_conf.Scheduler.Interval, ct);
                }
                _log.Information("Scheduler has finished");
                return Outcome.Success();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return Outcome.Fail(ex);
            }
        }

        async Task outputAsync(string text)
        {
            await _outputChannel!.SendMessageAsync(text);
        }

        public void AddPolicyOutcomeHandler(PolicyOutcomeHandler handler) 
        {
            _policyOutcomeHandlers.Add(handler);
        }

        public Scheduler(PolicyDispatcher policyDispatcher, DcafConfiguration conf, IDiscordGuild guild, ILog? log = null)
        {
            _policyDispatcher = policyDispatcher;
            _conf = conf;
            _guild = guild;
            _log = log;
        }
    }
}
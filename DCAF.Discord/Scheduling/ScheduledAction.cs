using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCAF.Discord.Policies;
using Discord.WebSocket;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging.Abstractions;
using TetraPak.XP.StringValues;

namespace DCAF.Discord.Scheduling
{
    [DebuggerDisplay("{Name}")]
    public sealed class ScheduledAction 
    {
        readonly object _syncRoot = new();
        TaskCompletionSource<bool>? _tcs;
        DateTime _nextRunTime;
        readonly IEnumerator<DateTime> _scheduledTimesEnumerator;

        public ILog? Log { get; }
        
        string Name { get; }

        public Dictionary<string,ConfigurationSectionDecorator> Args { get; }

        public override string ToString() => Name;

        public List<IPolicy>? ActionPolicies { get; private set; }

        internal void SetPolicies(List<IPolicy> policiesSequence)
        {
            ActionPolicies = policiesSequence;
        }

        internal Task RunIfScheduled(
            PolicyOutcomeHandler[] outcomeHandlers, 
            ISocketMessageChannel outputChannel)
        {
            lock (_syncRoot)
            {
                if (_tcs is { } && !_tcs.Task.IsCompleted)
                {
                    Log.Debug($"Action [{this}] was already running (ignoring)");
                    return Task.CompletedTask;
                }

                if (_nextRunTime >= XpDateTime.Now)
                {
                    Log.Debug($"Action [{this}] is scheduled to run {_nextRunTime} (waiting)'");
                    return Task.CompletedTask;
                }

                _nextRunTime = getNextRunTime();
                _tcs = new TaskCompletionSource<bool>(false);
            }

            var messageId = new RandomString();
            Log.Information($"Action [{this}] is starting (next run is scheduled at {_nextRunTime})", messageId);
            return Task.Run(() => runPoliciesAsync(outcomeHandlers, outputChannel, messageId));
        }

        DateTime getNextRunTime()
        {
            _scheduledTimesEnumerator.MoveNext();
            var next = _scheduledTimesEnumerator.Current;
            return next;
        }
        
        async Task runPoliciesAsync(
            PolicyOutcomeHandler[] outcomeHandlers,
            ISocketMessageChannel outputChannel, 
            string? messageId)
        {
            foreach (var policy in ActionPolicies!)
            {
                try
                {
                    Log.Information($"Action [{this}] executes policy '{policy}'", messageId);

                    Args.TryGetValue(policy.Name, out var configOverride);
                    var outcome = await policy.ExecuteAsync(configOverride);
                    if (outcome)
                    {
                        Log.Information($"Action [{this}] ended successfully", messageId);
                    }
                    else
                    {
                        var sb = new StringBuilder($"Action [{this}] was unsuccessful");
                        if (outcome.Exception is { })
                        {
                            sb.AppendLine(outcome.Exception.ToString());
                        }
                        Log.Warning(sb.ToString, messageId);
                    }
                    if (!outcomeHandlers.Any())
                        continue;

                    var args = new PolicyOutcomeArgs(policy, outcome, outputChannel, _nextRunTime);
                    foreach (var outcomeHandler in outcomeHandlers)
                    {
                        await outcomeHandler(args);
                    }
                }
                catch (Exception ex)
                {
                    ex = new Exception($"Error in action '{this}' running policy {policy.Name}: {ex.Message}", ex);
                    Log.Error(ex);
                }
            }
            _tcs!.SetResult(true);
        }
        
        public ScheduledAction(ScheduledActionConfiguration config)
        {
            Name = config.Key;
            Log = config.Log;
            Args = config.Args;
            if (!config.Enabled)
            {
                _nextRunTime = DateTime.MaxValue;
                _scheduledTimesEnumerator = null!;
                return;
            }
            
            if (config.Times is null)
            {
                _nextRunTime = DateTime.MaxValue;
                _scheduledTimesEnumerator = null!;
                return;
            }
                
            _scheduledTimesEnumerator = new InfiniteDateTimes(config.Weekdays, config.Times, XpDateTime.Now, config.Log).GetEnumerator();
            _nextRunTime = _scheduledTimesEnumerator.Current;
        }
    }

    public sealed class ScheduledActionConfiguration : ConfigurationSectionDecorator
    {
        public bool Enabled => this.Get(true);

        public DateTime[]? Times => this.Get<DateTime[]>();

        public WeekdaysCollection Weekdays => this.Get<WeekdaysCollection>() ?? WeekdaysCollection.Empty;

        public PoliciesSequence Policies => this.Get<PoliciesSequence>() ?? PoliciesSequence.Empty;

        /// <summary>
        ///   Dictionary containing overridden args for policies. This makes it possible to
        ///   run same policy at different times/intervals.
        /// </summary>
        public Dictionary<string,ConfigurationSectionDecorator> Args { get; }
        
        public ScheduledActionConfiguration(ConfigurationSectionDecoratorArgs args) 
        : base(args)
        {
            var argsSection = GetSection("Args");
            Args = new Dictionary<string, ConfigurationSectionDecorator>();
            if (!(argsSection?.IsConfigurationSection() ?? false))
                return;
            
            var subSections = argsSection.GetSubSections();
            foreach (var subSection in subSections)
            {
                Args[subSection.Key] = (ConfigurationSectionDecorator) subSection;
            }
        }
    }
}
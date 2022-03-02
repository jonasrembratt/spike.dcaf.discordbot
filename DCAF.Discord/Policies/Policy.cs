using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DCAF._lib;
using TetraPak.XP;
using TetraPak.XP.Logging;

namespace DCAF.Discord.Policies
{
    [DebuggerDisplay("{ToString()}")]
    public abstract class Policy<T> : IPolicy  where T : PolicyResult 
    {
        public PolicyDispatcher Dispatcher { get; }

        public ILog? Log { get; }

        public string Name { get; }
        
        // public abstract Task<Outcome<T>> ExecuteAsync(PolicyArgs args);

        public abstract Task<Outcome> ResetCacheAsync();

        public override string ToString() => $"{base.ToString()} ({Name})";

        public Policy(string name, PolicyDispatcher dispatcher, ILog? log)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Dispatcher = dispatcher;  
            dispatcher.Add(this);
            Log = log;
        }

    }

    public interface IPolicy
    {
        string Name { get; }

        Task<Outcome> ResetCacheAsync();
    }

    public class PolicyArgs 
    {
        public bool IsCliMessage { get; private set; }

        public string[] Parameters { get; private set; }

        public bool TryGetValue(out string? value, params string[] keys)
            =>
            Parameters.TryGetValue(out value, keys);

        public bool TryGetFlag(params string[] keys)
            =>
            Parameters.TryGetFlag(keys);

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public PolicyArgs WithParameters(params string[] parameters)
        {
            Parameters = parameters;
            return this;
        }

        public static PolicyArgs FromCli(string[] parameters)
        {
            return new PolicyArgs(parameters)
            {
                IsCliMessage = true,
            };
        }

        public static PolicyArgs FromCommand(string[] parameters)
        {
            return new PolicyArgs(parameters)
            {
                IsCliMessage = true,
            };
        }

        PolicyArgs(params string[] parameters) => WithParameters(parameters);
    }
}
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DCAF._lib;
using Microsoft.Extensions.Configuration;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Policies
{
    [DebuggerDisplay("{ToString()}")]
    public abstract class Policy : IPolicy 
    {
        // const string ConfigSectionKey = "Policies"; obsolete
        
        public PolicyDispatcher Dispatcher { get; }

        public ILog? Log { get; }

        public string Name { get; }

        public abstract Task<Outcome> ExecuteAsync(IConfiguration? config);
        
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

        Task<Outcome> ExecuteAsync(IConfiguration? config);
        
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
    }
}
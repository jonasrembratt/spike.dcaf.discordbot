using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;

namespace DCAF.DiscordBot.Policies
{
    [DebuggerDisplay("{ToString()}")]
    public abstract class Policy
    {
        public PolicyDispatcher Dispatcher { get; }

        public string Name { get; }
        public abstract Task<Outcome> ExecuteAsync();

        public abstract Task<Outcome> ResetCacheAsync();

        public override string ToString() => $"{base.ToString()} ({Name})";

        public Policy(string name, PolicyDispatcher dispatcher)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Dispatcher = dispatcher;
            dispatcher.Add(this);
        }
    }
}
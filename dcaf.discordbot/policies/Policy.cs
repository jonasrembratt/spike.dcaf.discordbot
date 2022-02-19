using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using Discord.WebSocket;
using TetraPak.XP.Logging;

namespace DCAF.DiscordBot.Policies
{
    [DebuggerDisplay("{ToString()}")]
    public abstract class Policy //: ModuleBase<SocketCommandContext> obsolete
    {
        public PolicyDispatcher Dispatcher { get; }

        public ILog? Log { get; }

        public string Name { get; }
        
        public abstract Task<Outcome> ExecuteAsync(PolicyArgs args);

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

    public class PolicyArgs 
    {
        public bool IsCliMessage { get; private set; }

        public SocketMessage SocketMessage { get; private set; }
        
        public string[] Parameters { get; private set; }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public static PolicyArgs FromCli(string[] parameters)
        {
            return new PolicyArgs
            {
                IsCliMessage = true,
                SocketMessage = null!,
                Parameters = parameters
            };
        }


        public static PolicyArgs FromSocketMessage(SocketMessage message)
        {
            return new PolicyArgs
            {
                IsCliMessage = false,
                SocketMessage = message,
                Parameters = makeParameters(message)
            };
        }

        static string[] makeParameters(SocketMessage message)
        {
            throw new NotImplementedException();
        }

        PolicyArgs()
        {
        }
    }
}
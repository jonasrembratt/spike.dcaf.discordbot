using System;
using System.Threading.Tasks;
using DCAF.DiscordBot._lib;

namespace DCAF.DiscordBot.Policies
{
    public abstract class Policy
    {
        public string Name { get; }
        public abstract Task<Outcome> ExecuteAsync();

        public Policy(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
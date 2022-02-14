using System.Threading.Tasks;
using DCAF.DiscordBot._lib;
using Discord.WebSocket;

namespace DCAF.DiscordBot.Policies
{
    public class ListPolicy : Policy
    {
        readonly DiscordSocketClient _client;
        const string CommandName = "get";
        
        public override Task<Outcome> ExecuteCliAsync(string[] args)
        {
            
        }

        public override Task<Outcome> ResetCacheAsync()
        {
            throw new System.NotImplementedException();
        }
        
        public ListPolicy(PolicyDispatcher dispatcher, DiscordSocketClient client) 
        : base(CommandName, dispatcher)
        {
            _client = client;
        }
    }
}
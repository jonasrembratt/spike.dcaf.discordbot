using System;
using System.Threading.Tasks;
using DCAF.DiscordBot.Policies;
using Discord.Commands;
using Discord.WebSocket;

namespace DCAF.DiscordBot.Commands
{
    [Group("personnel")]
    public class PersonnelModule : ModuleBase<SocketCommandContext>
    {
        readonly PolicyDispatcher _policyDispatcher;

        [Command("sync-ids")]
        [Summary("Ensures members in the Google sheet Personnel sheet are assigned correct Discord IDs")]
        public Task SyncIds(
            [Summary("Specifies whether all members of the Personnel sheet with unrecognized Discord names should be listed")]
            bool listUnrecognised = false)
        {
            throw new NotImplementedException();
        }

        public PersonnelModule(PolicyDispatcher policyDispatcher)
        {
            _policyDispatcher = policyDispatcher;
        }
    }
    
    [Group("sample")]
    public class SampleModule : ModuleBase<SocketCommandContext>
    {
        // ~sample square 20 -> 400
        [Command("square")]
        [Summary("Squares a number.")]
        public async Task SquareAsync(
            [Summary("The number to square.")] 
            int num)
        {
            // We can also access the channel from the Command Context.
            await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
        }

        // ~sample userinfo --> foxbot#0282
        // ~sample userinfo @Khionu --> Khionu#8708
        // ~sample userinfo Khionu#8708 --> Khionu#8708
        // ~sample userinfo Khionu --> Khionu#8708
        // ~sample userinfo 96642168176807936 --> Khionu#8708
        // ~sample whois 96642168176807936 --> Khionu#8708
        [Command("userinfo")]
        [Summary
            ("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync(
            [Summary("The (optional) user to get info from")]
            SocketUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }
    }
}
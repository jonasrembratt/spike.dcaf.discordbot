using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Google;
using DCAF.DiscordBot.Model;
using DCAF.DiscordBot.Policies;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace DCAF.DiscordBot.Services
{
    static class ServicesHelper
    {
        public static async Task<IServiceCollection> AddPersonnelAsync(this IServiceCollection collection)
        {
            await collection.AddGooglePersonnelSheetAsync();
            collection.AddSingleton<IPersonnel>(p =>
            {
                var personnelSheet = p.GetRequiredService<GooglePersonnelSheet>();
                return new DcafPersonnel(personnelSheet);
            });
            return collection;
        }
        
        public static async Task<IServiceCollection> AddGooglePersonnelSheetAsync(this IServiceCollection collection)
        {
            var args = new GoogleSheetArgs(
                "Personnel",
                "DCAF",
                "1YkknGcD9zkLK5WHkJhUalF-XQXbpx3ltzqM98wYJ6Bs");
            var sheet = await GoogleSheet.OpenAsync(args, new FileInfo("./google.credentials.json"));
            var personnelSheet = new GooglePersonnelSheet(sheet);
            collection.AddSingleton<GooglePersonnelSheet>(p => new GooglePersonnelSheet(sheet));
            return collection;
        }

        public static IServiceCollection AddDiscordGuild(
            this IServiceCollection collection, 
            DiscordSocketClient discordClient, 
            ulong guildId)
        {
            collection.AddSingleton(p => new DiscordGuild(discordClient, guildId));
            return collection;
        }

        public static async Task<IServiceCollection> AddPoliciesAsync(this IServiceCollection collection)
        {
            var file = new FileInfo("./_files/events.csv");
            var loadEventsOutcome = await EventCollection.LoadFromAsync(file);
            if (!loadEventsOutcome)
                throw new Exception("Could not load events from file");

            collection.AddSingleton(p => loadEventsOutcome.Value!);
            collection.AddSingleton<PolicyDispatcher>();
            collection.AddSingleton<SynchronizeIdsPolicy>();
            collection.AddSingleton<ResetPoliciesPolicy>();
            collection.AddSingleton<SetAwolPolicy>();
            return collection;
        }

        public static void ActivatePolicies(this IServiceProvider provider)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var policyTypes = assembly.GetTypes().Where(t => !t.IsAbstract && typeof(Policy).IsAssignableFrom(t));
                foreach (var type in policyTypes)
                {
                    provider.GetService(type);
                }
            }
        }
    }
}
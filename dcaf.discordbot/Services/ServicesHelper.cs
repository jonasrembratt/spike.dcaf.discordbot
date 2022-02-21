using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dcaf.discordbot;
using DCAF.DiscordBot.Commands;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Google;
using DCAF.DiscordBot.Model;
using DCAF.DiscordBot.Policies;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.DependencyInjection;
using TetraPak.XP.Desktop;
using TetraPak.XP.Logging;
using TetraPk.XP.Web.Http;

namespace DCAF.DiscordBot.Services
{
    static class ServicesHelper
    {
        public static async Task<IServiceProvider> ConfigureServicesAsync(
            this DiscordSocketClient client,
            CancellationTokenSource cts,
            bool isCliEnabled)
        {
            var collection = XpServices.BuildFor().Desktop().GetServiceCollection();
            collection.AddBasicLogging();
            collection.AddHttpClientProvider();
            collection.AddDcafConfiguration(isCliEnabled);
            collection.AddDiscordService(client, cts);
            await collection.AddPersonnelSheetAsync();
            collection.AddGuildEventsrepository();
            await collection.AddPoliciesAsync();
            collection.AddSingleton(p => client);
            collection.AddDiscordGuild(client);
            collection.AddSingleton<CommandService>();
            collection.AddSingleton<CommandHandler>();
            
            var services = collection.BuildServiceProvider();
            var commandHandler = services.GetRequiredService<CommandHandler>();
            await commandHandler.InstallCommandsAsync(services);
            return services;
        }

        public static IServiceCollection AddDcafConfiguration(this IServiceCollection collection, bool isCliEnabled)
        {
            collection.AddConfiguration();
            collection.AddSingleton(p =>
            {
                var configuration = p.GetRequiredService<IConfiguration>() as IConfigurationSectionExtended;
                return new DcafConfiguration(configuration!, isCliEnabled);
            });
            return collection;
        }

        public static IServiceCollection AddDiscordService(
            this IServiceCollection collection,
            DiscordSocketClient client, 
            CancellationTokenSource cts)
        {
            collection.AddSingleton(p => new DiscordService(client, cts));
            return collection;
        }

        public static IServiceCollection AddBasicLogging(this IServiceCollection collection)
        {
            // todo support Discord logging framework instead
            collection.AddSingleton(p => new BasicLog().WithConsoleLogging());
            return collection;
        }

        public static IServiceCollection AddHttpClientProvider(this IServiceCollection collection)
        {
            collection.AddSingleton<IHttpClientProvider, HttpClientProvider>();
            return collection;
        }

        public static IServiceCollection AddEventsRepository(this IServiceCollection collection)
        {
            collection.AddSingleton<GuildEventsRepository>();
            return collection;
        }
        
        public static async Task<IServiceCollection> AddPersonnelSheetAsync(this IServiceCollection collection)
        {
            await collection.AddGooglePersonnelSheetAsync();
            collection.AddSingleton<IPersonnel>(p =>
            {
                var personnelSheet = p.GetRequiredService<GooglePersonnelSheet>();
                return new DcafPersonnel(personnelSheet);
            });
            return collection;
        }

        public static IServiceCollection AddGuildEventsrepository(this IServiceCollection collection)
        {
            collection.AddSingleton<GuildEventsRepository>();
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
            DiscordSocketClient discordClient)
        {
            collection.AddSingleton<IDiscordGuild,DiscordGuild>(/* obsolete p =>
            {
                var config = p.GetRequiredService<DcafConfiguration>();
                var discord = p.GetRequiredService<IDiscordService>();
                return new DiscordGuild(discordClient, config.GetGuildId());
            }*/);
            return collection;
        }

        public static async Task<IServiceCollection> AddPoliciesAsync(this IServiceCollection collection)
        {
            var file = new FileInfo("./_files/events.csv");
            var loadEventsOutcome = await EventCollection.LoadFromAsync(file);
            if (!loadEventsOutcome)
                throw new Exception("Could not load events from file");

            collection.AddSingleton(p => loadEventsOutcome.Value!);
            collection.AddSingleton<CommandService>();
            collection.AddSingleton<PolicyDispatcher>();
            collection.AddSingleton<SynchronizePersonnelDiscordIdsPolicy>();
            collection.AddSingleton<ResetPolicy>();
            collection.AddSingleton<SetAwolPolicy>();
            // collection.AddSingleton<GetStuffPolicy>();
            return collection;
        }

        public static void ActivatePolicies(this IServiceProvider provider)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var policyTypes = assembly.GetTypes().Where(t => !t.IsAbstract && t.IsImplementingInterface<IPolicy>());
                foreach (var type in policyTypes)
                {
                    provider.GetService(type);
                }
            }
        }
    }
}
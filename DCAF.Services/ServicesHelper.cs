using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DCAF.Discord;
using DCAF.Discord.Commands;
using DCAF.Discord.Policies;
using DCAF.Google;
using DCAF.Model;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.DependencyInjection;
using TetraPak.XP.Desktop;
using TetraPak.XP.Logging;
using TetraPak.XP.Web.Http;

namespace DCAF.Services
{
    public static class ServicesHelper
    {
        public static async Task<IServiceProvider> BuildServicesAsync(
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
            collection.AddGuildEventsRepository();
            await collection.AddPoliciesAsync();
            collection.AddSingleton(p => client);
            collection.AddDiscordGuild();
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
            collection.AddSingleton<IPersonnel<DiscordMember>>(p =>
            {
                var personnelSheet = p.GetRequiredService<GooglePersonnelSheet>();
                return new DcafPersonnel(personnelSheet);
            });
            return collection;
        }

        public static IServiceCollection AddGuildEventsRepository(this IServiceCollection collection)
        {
            collection.AddSingleton<GuildEventsRepository>();
            return collection;
        }
        
        public static async Task<IServiceCollection> AddGooglePersonnelSheetAsync(this IServiceCollection collection)
        {
            // var args = new GoogleSheetArgs(
            //     "Personnel",
            //     "DCAF",
            //     "1YkknGcD9zkLK5WHkJhUalF-XQXbpx3ltzqM98wYJ6Bs");
            // var sheet = await GoogleSheet.OpenAsync(args, new FileInfo("./google.credentials.json"));
            collection.AddSingleton(p =>
            {
                var config = p.GetRequiredService<DcafConfiguration>();
                var sheetConfig = config.PersonnelSheet;
                if (sheetConfig is null)
                    throw new ConfigurationException(
                        $"No '{nameof(DcafConfiguration.PersonnelSheet)}' section found in configuration");
                
                var args = new GoogleSheetArgs(
                    sheetConfig.SheetName!,
                    sheetConfig.ApplicationName!,
                    sheetConfig.DocumentId!);
                var sheet = GoogleSheet.OpenAsync(args, new FileInfo("./google.credentials.json")).Result;
                return new GooglePersonnelSheet(sheet);
            });
            return collection;
        }

        public static IServiceCollection AddDiscordGuild(this IServiceCollection collection)
        {
            collection.AddSingleton<IDiscordGuild,DiscordGuild>();
            return collection;
        }

        public static async Task<IServiceCollection> AddPoliciesAsync(this IServiceCollection collection)
        {
            collection.AddSingleton<CommandService>();
            collection.AddSingleton<PolicyDispatcher>();
            collection.AddSingleton<SynchronizePersonnelIdsPolicy>();
            collection.AddSingleton<ResetPolicy>();
            collection.AddSingleton<AwolPolicy>();
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
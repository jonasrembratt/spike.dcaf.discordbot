using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DCAF.Discord;
using DCAF.Discord.Policies;
using DCAF.Google;
using DCAF.Model;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging;
using TetraPak.XP.Logging.Abstractions;
using TetraPak.XP.Web.Http;

namespace DCAF.Services
{
    public static class ServicesHelper
    {
        public static IServiceCollection AddServices(
            this IServiceCollection collection,
            DiscordSocketClient client,
            CancellationTokenSource cts,
            TaskCompletionSource<bool> clientReadyStateSource)
        {
            collection.AddBasicLogging();
            collection.UseHttpClientProvider();
            collection.AddDcafConfiguration();
            collection.AddDiscordService(client, cts, clientReadyStateSource);
            collection.UsePersonnelSheet();
            // collection.UseMemberApplicationSheet();
            collection.AddGuildEventsRepository();
            collection.AddPoliciesAsync();
            collection.AddSingleton(_ => client);
            collection.AddDiscordGuild();
            collection.AddSingleton<CommandService>();
            // collection.AddSingleton<CommandHandler>(); obsolete
            
            return collection;
        }

        // public static IServiceCollection UseDcafConfiguration(this IServiceCollection collection) obsolete?
        // {
        //     collection.AddSingleton(p =>
        //     {
        //         var args = ConfigurationSectionWrapperArgs.ForSubSection(null, DcafConfiguration.SectionKey);
        //         return new DcafConfiguration(args);
        //     });
        //     return collection;
        // }

        public static IServiceCollection AddDiscordService(
            this IServiceCollection collection,
            DiscordSocketClient client, 
            CancellationTokenSource cts,
            TaskCompletionSource<bool> clientReaderStateSource)
        {
            collection.AddSingleton(_ => new DiscordService(client, cts, clientReaderStateSource));
            return collection;
        }

        public static IServiceCollection AddBasicLogging(this IServiceCollection collection)
        {
            // todo support Discord logging framework instead
            collection.AddSingleton(p 
                => 
                new LogBase(p.GetRequiredService<IConfiguration>())
                    .WithConsoleLogging());
            return collection;
        }

        public static IServiceCollection UseHttpClientProvider(this IServiceCollection collection)
        {
            collection.AddSingleton<IHttpClientProvider, HttpClientProvider>();
            return collection;
        }

        public static IServiceCollection UseEventsRepository(this IServiceCollection collection)
        {
            collection.AddSingleton<GuildEventsRepository>();
            return collection;
        }
        
        public static IServiceCollection UsePersonnelSheet(this IServiceCollection collection)
        {
            collection.AddGooglePersonnelSheetAsync();
            collection.AddSingleton<IPersonnel<DiscordMember>>(p =>
            {
                var personnelSheet = p.GetRequiredService<GooglePersonnelSheet>();
                return new DcafPersonnel(personnelSheet);
            });
            return collection;
        }

        public static IServiceCollection UseMemberApplicationSheet(this IServiceCollection collection)
        {
            collection.AddGoogleMemberApplicationsSheetAsync();
            return collection;
        }

        public static IServiceCollection AddGuildEventsRepository(this IServiceCollection collection)
        {
            collection.AddSingleton<GuildEventsRepository>();
            return collection;
        }
        
        public static IServiceCollection AddGooglePersonnelSheetAsync(this IServiceCollection collection)
        {
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
                var credentialsFile = new FileInfo("./google.credentials.json");
                var sheet = GoogleSheet.OpenAsync(args, credentialsFile).Result;
                // var sheet = GoogleSheet.OpenAsync(args, new FileInfo("./google.credentials.json")).Result;
                return new GooglePersonnelSheet(sheet, p.GetService<ILog>());
            });
            return collection;
        }
        
        public static IServiceCollection AddGoogleMemberApplicationsSheetAsync(this IServiceCollection collection)
        {
            collection.AddSingleton(p =>
            {
                var config = p.GetRequiredService<DcafConfiguration>();
                var sheetConfig = config.MemberApplicationSheet;
                if (sheetConfig is null)
                    throw new ConfigurationException(
                        $"No '{nameof(DcafConfiguration.PersonnelSheet)}' section found in configuration");
                
                var args = new GoogleSheetArgs(
                    sheetConfig.SheetName!,
                    sheetConfig.ApplicationName!,
                    sheetConfig.DocumentId!);
                var credentialsFile = new FileInfo("./google.credentials.json");
                var sheet = GoogleSheet.OpenAsync(args, credentialsFile).Result;
                // var sheet = GoogleSheet.OpenAsync(args, new FileInfo("./google.credentials.json")).Result;
                return new GoogleMemberApplicationSheet(sheet, p.GetService<ILog>());
            });
            return collection;
        }

        public static IServiceCollection AddDiscordGuild(this IServiceCollection collection)
        {
            collection.AddSingleton<IDiscordGuild,DiscordGuild>();
            return collection;
        }

        public static IServiceCollection AddPoliciesAsync(this IServiceCollection collection)
        {
            collection.AddSingleton<CommandService>();
            collection.AddSingleton<PolicyDispatcher>();
            collection.AddSingleton<MemberSyncIdsPolicy>();
            collection.AddSingleton<ResetPolicy>();
            collection.AddSingleton<MemberStatusPolicy>();
            collection.AddSingleton<CleanupChannelPolicy>();
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
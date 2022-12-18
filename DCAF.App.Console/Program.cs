using System;
using System.Threading;
using System.Threading.Tasks;
using DCAF.Discord;
using DCAF.Discord.Commands;
using DCAF.Discord.Conversations;
using DCAF.Discord.Policies;
using DCAF.Discord.Scheduling;
using DCAF.Model;
using DCAF.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TetraPak.XP;
using TetraPak.XP.ApplicationInformation;
using TetraPak.XP.Configuration;
using TetraPak.XP.DependencyInjection;
using TetraPak.XP.Desktop;
using TetraPak.XP.Logging;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.App.Console
{
    class Program
    {
        const string DcafBotTokenIdent = "DCAF_BOT_TOKEN"; 
        // const string KeyWord = "!dcaf"; obsolete
        static bool s_isPoliciesAvailable;
        static bool s_isPersonnelReady;
        
        static DiscordSocketClient? s_client;
        static ILog? s_log;
        static IServiceProvider s_services = null!;
        static Scheduler s_scheduler;
        static readonly CancellationTokenSource s_cts = new();
        static TaskCompletionSource<bool> s_clientStateSource;

        static async Task Main(string[] args)
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
            };
            s_client = new DiscordSocketClient(config);
            s_client.Log += onLog;
            
            // var isCliEnabled = isCli(args, out var command, out var cliParams); 
            
            s_clientStateSource = new TaskCompletionSource<bool>();
            await initHostAsync(args, s_client); 
            s_log = s_services.GetService<ILog>();
            var dcafConfig = s_services.GetRequiredService<DcafConfiguration>();
            var personnel = s_services.GetRequiredService<IPersonnel<DiscordMember>>();
            personnel.Ready += (_, _) => s_isPersonnelReady = true;
            s_client.Ready += async () =>
            {
                s_clientStateSource.SetResult(true);
                s_services.ActivatePolicies();
                s_isPoliciesAvailable = true;
                s_scheduler = s_services.GetRequiredService<Scheduler>();
                var interactions = s_services.GetRequiredService<InteractionHandler>();
                await interactions.InitializeAsync();
#pragma warning disable CS4014
                s_scheduler.RunAsync(s_cts);
#pragma warning restore CS4014
            };
            var botToken = getBotToken(dcafConfig);
            if (string.IsNullOrWhiteSpace(botToken))
            {
                s_log.Error(new ConfigurationException("Cannot find a bot token in configuration file or environment variable"));
                return;
            }
                
            await s_client.LoginAsync(TokenType.Bot, botToken);
            await s_client.StartAsync();

            System.Console.WriteLine("Type 'quit' and [ENTER] to terminate bot");
            var cmd = System.Console.ReadLine()?.ToLowerInvariant();
            while (cmd is null or not "quit")
            {
                cmd = System.Console.ReadLine()?.ToLowerInvariant();
            }
            s_cts.Cancel();
            
            // waitForClientToGetReady(); obsolete
            // var policyDispatcher = s_services.GetRequiredService<PolicyDispatcher>();
            // s_log.Information("Bot is running in local CLI mode ...");
            //
            // command ??= promptForPolicy(out cliParams);
            // var policyArgs = PolicyArgs.FromCli(cliParams);
            // while(!command?.Equals("quit") ?? false)
            // {
            //     if (string.IsNullOrWhiteSpace(command))
            //     {
            //         command = promptForPolicy(out cliParams);
            //         continue;
            //     }
            //
            //     var guild = s_services.GetRequiredService<IDiscordGuild>();
            //     var botName = dcafConfig.BotName;
            //     if (botName is null)
            //         throw new ConfigurationException(
            //             $"CLI is enabled but no {nameof(DcafConfiguration.BotName)} was specified in configuration");
            //             
            //     var botOutcome = await guild.GetUserWithDiscordNameAsync(botName);    
            //     if (!botOutcome)
            //         throw new ConfigurationException(
            //             $"CLI is enabled but configured bot is not recognized for this server: {botName}");
            //
            //     var bot = botOutcome.Value!;
            //     // await bot.SendMessageAsync(message);
            //     
            //     // if (!policyDispatcher.TryGetPolicy(command, out var policy))
            //     // {
            //     //     s_log.Warning($"Unknown policy: {command}");
            //     //     command = promptForPolicy(out cliParams);
            //     //     continue;
            //     // }
            //     //
            //     // var outcome = await policy.ExecuteAsync(policyArgs);
            //     // if (!outcome)
            //     // {
            //     //     s_log.Error(outcome.Exception!);
            //     // }
            //     // else
            //     // {
            //     //     s_log.Information($"{policy.Name} completed successfully{(outcome.HasMessage ? $": {outcome.Message}" : "" )}");
            //     // }
            //     // command = promptForPolicy(out cliParams);
            //     // policyArgs = PolicyArgs.FromCli(cliParams);
            // }
            //
            // await s_client.LogoutAsync();
        }

        static async Task initHostAsync(string[] args, DiscordSocketClient discordClient)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(collection =>
                {
                    s_services = XpServices
                        .BuildFor().Desktop(ApplicationPlatform.Console).WithServiceCollection(collection)
                        .AddXpDateTime()
                        .AddServices(s_client!, new CancellationTokenSource(), s_clientStateSource)
                        .AddDcafConfiguration()
                        .AddSingleton<PolicyDispatcher>()
                        .AddScheduler()
                        .AddSingleton(_ => discordClient)
                        .AddSingleton(p => new InteractionService(p.GetRequiredService<DiscordSocketClient>()))
                        .AddSingleton<InteractionHandler>()
                        .AddSingleton<ConversationManager>()
                        .AddSingleton( p =>
                        {
                            var rank = resolveLogRank(p, LogRank.Information);
                            var log = new LogBase(p.GetRequiredService<IConfiguration>()) { Rank = rank } .WithConsoleLogging();
                            return log;

                        })
                        .BuildXpServices();
                    s_log = s_services.GetService<ILog>();
                })
                .ConfigureHostConfiguration(builder =>
                {
                    builder.AddEnvironmentVariables();
                })
                .ConfigureAppConfiguration((_, builder) => builder.Build())
                .Build();

            // var commandHandler = s_services.GetRequiredService<CommandHandler>(); // obsolete
            s_services.GetRequiredService<InteractionHandler>();
            // await commandHandler.InstallCommandsAsync(s_services); obsolete
        }

        static string? getBotToken(IConfigurationSection dcafConfig)
        {
            var token = dcafConfig.GetValue<string>("BotToken") 
                        ?? Environment.GetEnvironmentVariable(DcafBotTokenIdent);
            if (string.IsNullOrWhiteSpace(token))
                return token;

            return token.StartsWith("421771-") 
                ? token["421771-".Length..].Replace("#", ".") 
                : token;
        }

        static LogRank resolveLogRank(IServiceProvider p, LogRank useDefault)
        {
            var config = p.GetRequiredService<IConfiguration>();
            var logLevelSection = config.GetSubSection(new ConfigPath(new[] { "Logging", "LogLevel" }));
            if (logLevelSection is null)
                return useDefault;

            var s = logLevelSection.Get<string>("Default");
            if (string.IsNullOrEmpty(s))
                return useDefault;
            
            return s.TryParseEnum(typeof(LogRank), out var obj) && obj is LogRank logRank
                ? logRank
                : useDefault;
        }

        static async Task<Outcome> isUserAllowedAsync(SocketMessage msg)
        {
            if (msg.Author.IsBot)
                return Outcome.Fail(new Exception("Messages from bots are not allowed"));

            var guild = s_services.GetRequiredService<IDiscordGuild>();
            var socketGuild = await guild.GetSocketGuildAsync();
            var user = socketGuild.GetUser(msg.Author.Id);
            var roles = user.Roles;

            throw new NotImplementedException();

        }

        static Task onLog(LogMessage msg)
        {
            var rank = msg.Severity.ToLogRank();
            s_log?.Write(rank, msg.Message, msg.Exception);
            return Task.CompletedTask;
        } 
    }

    static class LogHelper
    {
        public static LogRank ToLogRank(this LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    return LogRank.Error;
                
                case LogSeverity.Warning:
                    return LogRank.Warning;
                
                case LogSeverity.Info:
                    return LogRank.Information;
                
                case LogSeverity.Verbose:
                    return LogRank.Debug;
                
                case LogSeverity.Debug:
                    return LogRank.Trace;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }
    }
}
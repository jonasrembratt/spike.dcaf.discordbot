using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DCAF._lib;
using DCAF.Discord;
using DCAF.Discord.Policies;
using DCAF.Model;
using DCAF.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging;

namespace DCAF.App.Console
{
    class Program
    {
        const string DcafBotTokenIdent = "DCAF_BOT_TOKEN"; 
        const string KeyWord = "!dcaf";
        static bool s_isPoliciesAvailable;
        static bool s_isPersonnelReady;
        
        static DiscordSocketClient? s_client;
        static ILog? s_log;
        static IServiceProvider s_services;

        static async Task Main(string[] args)
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
             };
            var cts = new CancellationTokenSource();
            s_client = new DiscordSocketClient(config);
            s_client.Log += onLog;
            
            var isCliEnabled = isCli(args, out var command, out var cliParams); 
            s_services = await s_client.BuildServicesAsync(cts, isCliEnabled);
            s_log = s_services.GetService<ILog>();
            var dcafConfig = s_services.GetRequiredService<DcafConfiguration>();
            var personnel = s_services.GetRequiredService<IPersonnel<DiscordMember>>();
            personnel.Ready += (_, _) => s_isPersonnelReady = true;
            s_client.Ready += () =>
            {
                var discord = s_services.GetRequiredService<DiscordService>();
                discord.SetReady();
                s_services.ActivatePolicies();
                s_isPoliciesAvailable = true;
                return Task.CompletedTask;
            };
            var botToken = getBotToken(dcafConfig);
            if (string.IsNullOrWhiteSpace(botToken))
            {
                s_log.Error(new ConfigurationException("Cannot find a bot token in configuration file or environment variable"));
                return;
            }
                
            await s_client.LoginAsync(TokenType.Bot, botToken);
            await s_client.StartAsync();

            if (!isCliEnabled)
            {
                System.Console.WriteLine("Type 'quit' and [ENTER] to terminate bot");
                var cmd = System.Console.ReadLine()?.ToLowerInvariant();
                while (cmd is null or not "quit")
                {
                    cmd = System.Console.ReadLine()?.ToLowerInvariant();
                }
                return;
            }
            
            waitForClientToGetReady();
            var policyDispatcher = s_services.GetRequiredService<PolicyDispatcher>();
            s_log.Information("Bot is running in local CLI mode ...");
            
            command ??= promptForPolicy(out cliParams);
            var policyArgs = PolicyArgs.FromCli(cliParams);
            while(!command?.Equals("quit") ?? false)
            {
                if (string.IsNullOrWhiteSpace(command))
                {
                    command = promptForPolicy(out cliParams);
                    continue;
                }

                var guild = s_services.GetRequiredService<IDiscordGuild>();
                var botName = dcafConfig.BotName;
                if (botName is null)
                    throw new ConfigurationException(
                        $"CLI is enabled but no {nameof(DcafConfiguration.BotName)} was specified in configuration");
                        
                var botOutcome = await guild.GetUserWithDiscordNameAsync(botName);    
                if (!botOutcome)
                    throw new ConfigurationException(
                        $"CLI is enabled but configured bot is not recognized for this server: {botName}");

                var bot = botOutcome.Value!;
                // await bot.SendMessageAsync(message);
                
                // if (!policyDispatcher.TryGetPolicy(command, out var policy))
                // {
                //     s_log.Warning($"Unknown policy: {command}");
                //     command = promptForPolicy(out cliParams);
                //     continue;
                // }
                //
                // var outcome = await policy.ExecuteAsync(policyArgs);
                // if (!outcome)
                // {
                //     s_log.Error(outcome.Exception!);
                // }
                // else
                // {
                //     s_log.Information($"{policy.Name} completed successfully{(outcome.HasMessage ? $": {outcome.Message}" : "" )}");
                // }
                // command = promptForPolicy(out cliParams);
                // policyArgs = PolicyArgs.FromCli(cliParams);
            }

            await s_client.LogoutAsync();
        }

        static string? getBotToken(IConfigurationSectionExtended dcafConfig)
        {
            var token = dcafConfig.GetValue<string>("BotToken") 
                        ?? Environment.GetEnvironmentVariable(DcafBotTokenIdent);
            if (string.IsNullOrWhiteSpace(token))
                return token;

            return token.StartsWith("421771-") 
                ? token["421771-".Length..].Replace("#", ".") 
                : token;
        }

        static void waitForClientToGetReady()
        {
            while (!s_isPoliciesAvailable || !s_isPersonnelReady)
            {
                Task.Delay(100);
            }
        }

        static string? promptForPolicy(out string[] args)
        {
            System.Console.Write("Please enter name of policy:");
            var s = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s))
            {
                args = Array.Empty<string>();
                return s;
            }
            
            var split = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 1)
            {
                args = Array.Empty<string>();
                return split[0].Trim();
            }

            args = new string[split.Length - 1];
            for (int i = 1; i < split.Length; i++)
            {
                args[i - 1] = split[i].Trim();
            }

            return split[0].Trim();
        }

        static bool isCli(string[] args, out string? policyName, out string[] policyArgs)
        {
            policyArgs = Array.Empty<string>();
            if (!args.Any(i => i.Equals("cli", StringComparison.InvariantCultureIgnoreCase)))
            {
                policyName = null;
                return false;
            }

            policyName = args.Length > 1 ? args[1] : null;
            if (args.Length > 1)
            {
                policyArgs = new string[args.Length - 2];
                policyArgs.CopyFrom(args, 2);
            }
            
            return true;
        }

        static async Task onMessageReceived(SocketMessage msg) // todo consider supporting the Commands framework instead
        {
            if (!msg.Content.StartsWith(KeyWord))
                return;

            var raidHelperName = new DiscordName("Raid-Helper", "3806");
            var attachments = msg.Attachments.FirstOrDefault();
            
            if (!await isUserAllowedAsync(msg))
            {
                await msg.Channel.SendMessageAsync("Sorry my fiend but you're not allowed to order this bot around!");
                return;
            }

            throw new NotImplementedException();
                
            // var policyArgs = Array.Empty<string>();
            // var policyName = getPolicyName(msg.Content);
            //
            // var policyName ??= promptForPolicy(out policyArgs);
            // while(!policyName?.Equals("quit") ?? false)
            // {
            //     if (!policyDispatcher.TryGetPolicy(policyName, out var policy))
            //     {
            //         Console.WriteLine($"Unknown policy: {policyName}");
            //         policyName = promptForPolicy(out policyArgs);
            //         continue;
            //     }
            //
            //     var outcome = await policy.ExecuteAsync(policyArgs);
            //     Console.WriteLine(!outcome 
            //         ? outcome.Exception!.Message 
            //         : $"{policy.Name} completed successfully{(outcome.HasMessage ? $": {outcome.Message}" : "" )}");
            //     policyName = promptForPolicy(out policyArgs);
            // }
            //
            //
            // await msg.Channel.SendMessageAsync($"Received command from {msg.Author.Username} (#{msg.Channel.Name} / {msg.Channel.Id})");
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
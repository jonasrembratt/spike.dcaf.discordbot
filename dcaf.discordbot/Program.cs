using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DCAF.DiscordBot;
using DCAF.DiscordBot._lib;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Policies;
using DCAF.DiscordBot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TetraPak.XP.Logging;

namespace dcaf.discordbot
{
    class Program
    {
        const string DcafBotTokenIdent = "DCAF_BOT_TOKEN"; 
        // internal const ulong DcafGuildId = 272872335608905728;
        const string KeyWord = "!dcaf";
        static bool s_isPoliciesAvailable;
        static bool s_isPersonnelReady;
        
        static DiscordSocketClient _client;
        static ILog? _log;

        static async Task Main(string[] args)
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.Guilds
            };
            var cts = new CancellationTokenSource();
            _client = new DiscordSocketClient(config);
            _client.Log += onLog;
            _client.MessageReceived += onMessageReceived;
            var isCliEnabled = isCli(args, out var policyName, out var cliParams); 
            var services = await ServicesHelper.SetupServicesAsync(_client, cts, isCliEnabled);
            _log = services.GetService<ILog>();
            var personnel = services.GetRequiredService<IPersonnel>();
            personnel.Ready += (_, _) => s_isPersonnelReady = true;
            _client.Ready += () =>
            {
                var discord = services.GetRequiredService<DiscordService>();
                discord.ClientIsReady();
                services.ActivatePolicies();
                s_isPoliciesAvailable = true;
                return Task.CompletedTask;
            };
            var botToken = Environment.GetEnvironmentVariable(DcafBotTokenIdent);
            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();

            if (!isCliEnabled)
            {
                await Task.Delay(-1, cts.Token);
                return;
            }
            
            waitForClientToGetReady();
            var policyDispatcher = services.GetRequiredService<PolicyDispatcher>();
            _log.Information("Bot is running in local CLI mode ...");
            
            policyName ??= promptForPolicy(out cliParams);
            var policyArgs = PolicyArgs.FromCli(cliParams);
            while(!policyName?.Equals("quit") ?? false)
            {
                if (!policyDispatcher.TryGetPolicy(policyName, out var policy))
                {
                    _log.Warning($"Unknown policy: {policyName}");
                    policyName = promptForPolicy(out cliParams);
                    continue;
                }

                var outcome = await policy.ExecuteAsync(policyArgs);
                if (!outcome)
                {
                    _log.Error(outcome.Exception!);
                }
                else
                {
                    _log.Information($"{policy.Name} completed successfully{(outcome.HasMessage ? $": {outcome.Message}" : "" )}");
                }
                policyName = promptForPolicy(out cliParams);
                policyArgs = PolicyArgs.FromCli(cliParams);
            }

            await _client.LogoutAsync();
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
            Console.Write("Please enter name of policy:");
            var s = Console.ReadLine();
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
            
            return;
            // if (!isUserAllowed(msg))
            // {
            //     await msg.Channel.SendMessageAsync("Sorry my fiend but you're not allowed to order this bot around!");
            //     return;
            // }
            //     
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

            
            // await msg.Channel.SendMessageAsync($"Received command from {msg.Author.Username} (#{msg.Channel.Name} / {msg.Channel.Id})");
        }

        // static bool isUserAllowed(SocketMessage msg)
        // {
        //     msg.Author.
        // }

        static Task onLog(LogMessage msg)
        {
            var rank = msg.Severity.ToLogRank();
            _log?.Write(rank, msg.Message, msg.Exception);
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
using System;
using System.Linq;
using System.Threading.Tasks;
using DCAF.DiscordBot;
using DCAF.DiscordBot._lib;
using dcaf.discordbot.Discord;
using DCAF.DiscordBot.Policies;
using DCAF.DiscordBot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TetraPak.XP.DependencyInjection;
using TetraPak.XP.Desktop;
using TetraPak.XP.Logging;

namespace dcaf.discordbot
{
    class Program
    {
        const string DcafBotTokenIdent = "DCAF_BOT_TOKEN"; 
        internal const ulong DcafGuildId = 272872335608905728;
        const string KeyWord = "!dcaf";
        static bool s_isPoliciesAvailable;
        static bool s_isPersonnelReady;
        
        static DiscordSocketClient _client;

        // public static IServiceProvider Services { get; private set; }

        static async Task Main(string[] args)
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.Guilds
            };
            _client = new DiscordSocketClient(config);
            _client.Log += onLog;
            _client.MessageReceived += onMessageReceived;
            var services = await setupServicesAsync(_client, DcafGuildId);
            var personnel = services.GetRequiredService<IPersonnel>();
            personnel.Ready += (_, _) => s_isPersonnelReady = true;
            _client.Ready += () =>
            {
                services.ActivatePolicies();
                s_isPoliciesAvailable = true;
                return Task.CompletedTask;
            };
            var botToken = Environment.GetEnvironmentVariable(DcafBotTokenIdent);
            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();

            if (!isCli(args, out var policyName, out var cliParams))
            {
                await Task.Delay(-1);
                return;
            }
            
            waitForClientToGetReady();
            var policyDispatcher = services.GetRequiredService<PolicyDispatcher>();
            Console.WriteLine("Bot is running in local CLI mode ...");
            
            policyName ??= promptForPolicy(out cliParams);
            var policyArgs = PolicyArgs.FromCli(cliParams);
            while(!policyName?.Equals("quit") ?? false)
            {
                if (!policyDispatcher.TryGetPolicy(policyName, out var policy))
                {
                    Console.WriteLine($"Unknown policy: {policyName}");
                    policyName = promptForPolicy(out cliParams);
                    continue;
                }

                var outcome = await policy.ExecuteAsync(policyArgs);
                Console.WriteLine(!outcome 
                    ? outcome.Exception!.Message 
                    : $"{policy.Name} completed successfully{(outcome.HasMessage ? $": {outcome.Message}" : "" )}");
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

        static async Task<IServiceProvider> setupServicesAsync(DiscordSocketClient client, ulong guildId)
        {
            var collection = XpServices.BuildFor().Desktop().GetServiceCollection();
            collection.AddSingleton(_client);
            collection.AddHttpClientProvider();
            collection.AddBasicLogging();
            await collection.AddPersonnelAsync();
            await collection.AddPoliciesAsync();
            collection.AddDiscordGuild(client, guildId);
            return collection.BuildServiceProvider();
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"===> [Bot] {msg.ToString()}");
            Console.ResetColor();
            return Task.CompletedTask;
        } 
    }
}
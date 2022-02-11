using System;
using System.Linq;
using System.Threading.Tasks;
using DCAF.DiscordBot;
using DCAF.DiscordBot.Policies;
using DCAF.DiscordBot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

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

            if (!isCli(args, out var policyName))
            {
                await Task.Delay(-1);
                return;
            }
            
            waitForClientToGetReady();
            var policyDispatcher = services.GetRequiredService<PolicyDispatcher>();
            Console.WriteLine("Bot is running in local CLI mode ...");
            
            policyName ??= promptForPolicy();
            while(!policyName?.Equals("quit") ?? false)
            {
                if (!policyDispatcher.TryGetPolicy(policyName, out var policy))
                {
                    Console.WriteLine($"Unknown policy: {policyName}");
                    policyName = promptForPolicy();
                    continue;
                }

                var outcome = await policy.ExecuteAsync();
                Console.WriteLine(!outcome 
                    ? outcome.Exception!.Message 
                    : $"{policy.Name} completed successfully{(outcome.HasMessage ? $": {outcome.Message}" : "" )}");
                policyName = promptForPolicy();
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

        static string? promptForPolicy()
        {
            Console.Write("Please enter name of policy:");
            return Console.ReadLine();
        }

        static bool isCli(string[] args, out string? policyName)
        {
            if (!args.Any(i => i.Equals("cli", StringComparison.InvariantCultureIgnoreCase)))
            {
                policyName = null;
                return false;
            }

            policyName = args.Length > 1 ? args[1] : null;
            return true;
        }

        static async Task<IServiceProvider> setupServicesAsync(DiscordSocketClient client, ulong guildId)
        {
            var collection = new ServiceCollection();
            collection.AddSingleton(_client);
            await collection.AddPersonnelAsync();
            await collection.AddPoliciesAsync();
            collection.AddDiscordGuild(client, guildId);
            return collection.BuildServiceProvider();
        }

        static async Task onMessageReceived(SocketMessage msg)
        {
            if (!msg.Content.StartsWith(KeyWord))
                return;

            // await msg.Channel.SendMessageAsync($"Received command from {msg.Author.Username} (#{msg.Channel.Name} / {msg.Channel.Id})");
        }

        static Task onLog(LogMessage msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"===> [Bot] {msg.ToString()}");
            Console.ResetColor();
            return Task.CompletedTask;
        } 
    }
}
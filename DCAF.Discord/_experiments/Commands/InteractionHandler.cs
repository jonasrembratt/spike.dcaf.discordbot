using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class InteractionHandler
{
    // readonly DiscordSocketClient _client;
    readonly DiscordService _discordService;
    readonly InteractionService _commands;
    readonly IServiceProvider _services;
    readonly IDiscordGuild _guild;
    readonly ILog? _log;

    public async Task InitializeAsync()
    {
        await _commands.AddModulesAsync(typeof(InteractionHandler).Assembly, _services);
        var client = await _discordService.GetReadyClientAsync();
        client.InteractionCreated += onInteraction;
        _commands.Log += (msg) =>
        {
            _log.Trace($"{msg.Message} ({msg.Source})");
            return Task.CompletedTask;
        };
        await _commands.RegisterCommandsToGuildAsync(_guild.GuildId);
    }
    
    async Task onInteraction(SocketInteraction interaction)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            var client = await _discordService.GetReadyClientAsync();
            var context = new SocketInteractionContext(client, interaction);

            // Execute the incoming command.
            var result = await _commands.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    public InteractionHandler(
        IServiceProvider services,
        IDiscordGuild guild,
        DiscordService discordService,
        InteractionService commands,
        ILog? log = null)
    {
        _services = services;
        _guild = guild;
        _commands = commands;
        _discordService = discordService;
        _log = log;
    }
}
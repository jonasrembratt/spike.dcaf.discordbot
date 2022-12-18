using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace DCAF.Discord.Conversations;

public abstract class InteractionAbstraction : IDiscordInteraction
{
    readonly IDiscordInteraction _discordInteraction;

    public ulong Id => _discordInteraction.Id;


    public Task RespondAsync(string text = null!, Embed[] embeds = null!, bool isTTS = false, bool ephemeral = false,
        AllowedMentions allowedMentions = null!, MessageComponent components = null!, Embed embed = null!,
        RequestOptions options = null!)
    {
        return _discordInteraction.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task RespondWithFileAsync(Stream fileStream, string fileName, string text = null!, Embed[] embeds = null!,
        bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null!,
        MessageComponent components = null!, Embed embed = null!, RequestOptions options = null!)
    {
        return _discordInteraction.RespondWithFileAsync(fileStream, fileName, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task RespondWithFileAsync(string filePath, string fileName = null!, string text = null!, Embed[] embeds = null!,
        bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null!,
        MessageComponent components = null!, Embed embed = null!, RequestOptions options = null!)
    {
        return _discordInteraction.RespondWithFileAsync(filePath, fileName, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task RespondWithFileAsync(FileAttachment attachment, string text = null!, Embed[] embeds = null!, bool isTTS = false,
        bool ephemeral = false, AllowedMentions allowedMentions = null!, MessageComponent components = null!,
        Embed embed = null!, RequestOptions options = null!)
    {
        return _discordInteraction.RespondWithFileAsync(attachment, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task RespondWithFilesAsync(IEnumerable<FileAttachment> attachments, string text = null!, Embed[] embeds = null!, bool isTTS = false,
        bool ephemeral = false, AllowedMentions allowedMentions = null!, MessageComponent components = null!,
        Embed embed = null!, RequestOptions options = null!)
    {
        return _discordInteraction.RespondWithFilesAsync(attachments, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task<IUserMessage> FollowupAsync(string text = null!, Embed[] embeds = null!, bool isTTS = false, bool ephemeral = false,
        AllowedMentions allowedMentions = null!, MessageComponent components = null!, Embed embed = null!,
        RequestOptions options = null!)
    {
        return _discordInteraction.FollowupAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task<IUserMessage> FollowupWithFileAsync(Stream fileStream, string fileName, string text = null!, Embed[] embeds = null!,
        bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null!,
        MessageComponent components = null!, Embed embed = null!, RequestOptions options = null!)
    {
        return _discordInteraction.FollowupWithFileAsync(fileStream, fileName, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task<IUserMessage> FollowupWithFileAsync(string filePath, string fileName = null!, string text = null!, Embed[] embeds = null!,
        bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null!,
        MessageComponent components = null!, Embed embed = null!, RequestOptions options = null!)
    {
        return _discordInteraction.FollowupWithFileAsync(filePath, fileName, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task<IUserMessage> FollowupWithFileAsync(FileAttachment attachment, string text = null!, Embed[] embeds = null!, bool isTTS = false,
        bool ephemeral = false, AllowedMentions allowedMentions = null!, MessageComponent components = null!,
        Embed embed = null!, RequestOptions options = null!)
    {
        return _discordInteraction.FollowupWithFileAsync(attachment, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task<IUserMessage> FollowupWithFilesAsync(IEnumerable<FileAttachment> attachments, string text = null!, Embed[] embeds = null!, bool isTTS = false,
        bool ephemeral = false, AllowedMentions allowedMentions = null!, MessageComponent components = null!,
        Embed embed = null!, RequestOptions options = null!)
    {
        return _discordInteraction.FollowupWithFilesAsync(attachments, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task<IUserMessage> GetOriginalResponseAsync(RequestOptions options = null!)
    {
        return _discordInteraction.GetOriginalResponseAsync(options);
    }

    public Task<IUserMessage> ModifyOriginalResponseAsync(Action<MessageProperties> func, RequestOptions options = null!)
    {
        return _discordInteraction.ModifyOriginalResponseAsync(func, options);
    }

    public Task DeleteOriginalResponseAsync(RequestOptions options = null!)
    {
        return _discordInteraction.DeleteOriginalResponseAsync(options);
    }

    public Task DeferAsync(bool ephemeral = false, RequestOptions options = null!)
    {
        return _discordInteraction.DeferAsync(ephemeral, options);
    }

    public Task RespondWithModalAsync(Modal modal, RequestOptions options = null!)
    {
        return _discordInteraction.RespondWithModalAsync(modal, options);
    }

    ulong IDiscordInteraction.Id => _discordInteraction.Id;

    public InteractionType Type => _discordInteraction.Type;

    public IDiscordInteractionData Data => _discordInteraction.Data;

    public string Token => _discordInteraction.Token;

    public int Version => _discordInteraction.Version;

    public bool HasResponded => _discordInteraction.HasResponded;

    public IUser User => _discordInteraction.User;

    public string UserLocale => _discordInteraction.UserLocale;

    public string GuildLocale => _discordInteraction.GuildLocale;

    public bool IsDMInteraction => _discordInteraction.IsDMInteraction;
    
    public ulong? ChannelId { get; }
    
    public ulong? GuildId { get; }
    
    public ulong ApplicationId { get; }

    ulong IEntity<ulong>.Id => ((IEntity<ulong>)_discordInteraction).Id;

    public DateTimeOffset CreatedAt => _discordInteraction.CreatedAt;
    
    public InteractionAbstraction(IInteractionContext interactionContext)
    {
        _discordInteraction = interactionContext.Interaction;
    }

}
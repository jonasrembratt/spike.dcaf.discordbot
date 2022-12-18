using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DCAF.Discord.Commands;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TetraPak.XP;
using TetraPak.XP.Configuration;
using TetraPak.XP.Logging.Abstractions;

namespace DCAF.Discord.Policies;

public sealed class CleanupChannelPolicy : Policy
{
    readonly CleanupChannelArgs _args;
    readonly DiscordService _discordService;

    const string PolicyName = "maintenance cleanup-channel";

    public override Task<Outcome> ExecuteAsync(IConfiguration? config) => ExecuteAsync( cloneWithOverrides(config));

    CleanupChannelArgs cloneWithOverrides(IConfiguration? overrides)
    {
        return overrides is null
            ? _args 
            : _args.WithOverrides<CleanupChannelArgs>(overrides);
    }

    internal async Task<Outcome> ExecuteAsync(CleanupChannelArgs args)
    {
        if (args.Channel == 0L)
            return Outcome<CleanupChannelResult>.Fail(new ConfigurationException("Channel was not specified"));
        
        var client = await _discordService.GetReadyClientAsync();
        if (await client.GetChannelAsync(args.Channel) is not ISocketMessageChannel channel)
            return Outcome<CleanupChannelResult>.Fail(new ConfigurationException($"Channel id not supported: {args.Channel.ToString()}"));
            
        var messagesOutcome = await channel.TryGetMessagesAsync(messages => 
        {
            return RetrieveItemsFilterArgs<IMessage>.ContinueWith(messages.Where(isIncluded).ToArray());
            
            bool isIncluded(IMessage message)
            {
                if (message.IsPinned && !args.IncludePinned)
                    return false;

                if (!message.IsInTimeFrame(args.TimeFrame))
                {
                    Log.Debug($"Ignores message (not in timeframe): {message}");
                    return false;
                }
                
                if (args.Except is null)
                    return true;

                if (message.Author is not SocketGuildUser guildUser)
                    return false;

                var nisse = !guildUser.IsIdentity(args.Except) && !guildUser.Roles.Any(r => r.IsIdentity(args.Except));
                return nisse;
            }
        });
        
        if (!messagesOutcome)
            return Outcome<CleanupChannelResult>.Success(
                new CleanupChannelResult("No messages needed to be removed", channel, args.Silent));
        
        return await deleteMessages(messagesOutcome.Value!.ToArray());
        
        async Task<Outcome> deleteMessages(IMessage[] messages)
        {
            var bulkDeleteMessages = messages.Where(m => !m.IsOld()).ToArray();
            var otherMessages = messages.Where(m => m.IsOld()).ToArray();
            var options = RequestOptions.Default.WithDefaultRateLimitHandler();
            try
            {
                if (bulkDeleteMessages.Any())
                {
                    Log.Debug($"Bulk deletes {bulkDeleteMessages.Length} messages:");
                    Log.Debug(() =>
                    {
                        var sb = new StringBuilder();
                        foreach (var message in bulkDeleteMessages)
                        {
                            sb.AppendLine($"    {message} (@{message.CreatedAt:s}) ...");
                        }

                        return sb.ToString();
                    });
                    if (!args.Simulate)
                    {
                        await ((ITextChannel)channel).DeleteMessagesAsync(bulkDeleteMessages, options);
                    }
                }

                var countDeleted = bulkDeleteMessages.Length;  
                Log.Debug($"Deletes {otherMessages.Length} additional messages ...");
                for (var i = 0; i < otherMessages.Length; i++)
                {
                    Log.Trace($"Deletes message {otherMessages[i]} (@{otherMessages[i].CreatedAt:s}) ...");
                    if (!args.Simulate)
                    {
                        await channel.DeleteMessageAsync(otherMessages[i], options);
                    }
                    ++countDeleted;
                    await Task.Delay(2000);
                }
                
                return Outcome<CleanupChannelResult>.Success(
                    new CleanupChannelResult(
                        $"{countDeleted} messages were removed", 
                        channel, 
                        args.Silent));
            }
            catch (Exception ex)
            {
                ex = new Exception($"Channel cleanup failed: {ex.Message}", ex);
                return Outcome<CleanupChannelResult>.Fail(ex);
            }
        }
    }

    public override Task<Outcome> ResetCacheAsync()
    {
        throw new NotImplementedException();
    }
    
    public CleanupChannelPolicy(PolicyDispatcher dispatcher, DiscordService discordService, ILog? log = null)
    : base(PolicyName, dispatcher, log)
    {
        _discordService = discordService;
        _args = new CleanupChannelArgs(
            ConfigurationSectionDecoratorArgs.ForSubSection($"DCAF:Policies:{PolicyName}"));
    }
}
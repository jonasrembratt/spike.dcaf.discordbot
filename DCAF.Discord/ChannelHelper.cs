using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TetraPak.XP;

namespace DCAF.Discord;

public static class ChannelHelper
{
    public static async Task<EnumOutcome<IMessage>> TryGetMessagesAsync(
        this ISocketMessageChannel channel,
        Func<IMessage[],RetrieveItemsFilterArgs<IMessage>>? predicate = null,
        TimeSpan? timeout = null)
    {
        var allMessages = new List<IMessage>();
        IMessage? fromMessage = null;
        var expireTime = timeout is {} ? DateTime.Now.Add(timeout.Value) : DateTime.MaxValue;
        var options = RequestOptions.Default.WithTimeout(timeout); 
        var isTimedOut = false;
        Direction? direction = null;
        do
        {
            var chunkMessages =
                direction is { }
                    ? await channel
                        .GetMessagesAsync(fromMessage, direction.Value, options: options)
                        .FlattenAsync()
                    : await channel.GetMessagesAsync(options: options).FlattenAsync();

            var messages = chunkMessages?.ToArray() ?? Array.Empty<IMessage>();
            if (!messages.Any())
                return done();

            var lastMessage = messages.Last();
            if (direction is null)
            {
                // this is the first chunk; set direction of consecutive requests ... 
                var firstMessage = messages.First();
                direction = firstMessage.CreatedAt > lastMessage.CreatedAt
                    ? Direction.Before
                    : Direction.After;
            }

            fromMessage = lastMessage;

            if (predicate is null)
            {
                allMessages.AddRange(messages);
                continue;
            }

            var predicateResult = predicate(messages); 
            messages = predicateResult.Items;
            allMessages.AddRange(messages);
            isTimedOut = isTimedOut || DateTime.Now >= expireTime;
            if (isTimedOut)
                return done();

            switch (predicateResult.Behavior)
            {
                case RetrieveItemsBehavior.Continue:
                    break;
                
                case RetrieveItemsBehavior.End:
                    return done();
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

        } while (!isTimedOut);

        return done();

        EnumOutcome<IMessage> done()
        {
            return isTimedOut
                ? EnumOutcome<IMessage>.Fail(new TimeoutException("The operation timed out"))
                : EnumOutcome<IMessage>.Success(allMessages);
        }
        
    }
}
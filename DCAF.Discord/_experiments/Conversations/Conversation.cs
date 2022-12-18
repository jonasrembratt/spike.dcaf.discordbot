using System.Threading.Tasks;
using Discord;

namespace DCAF.Discord.Conversations;

public abstract class Conversation : InteractionAbstraction
{
    public Task RespondWithMenuAsync(
        SelectMenuComponent menu, 
        string text = null!,
        Embed[] embeds = null!, 
        bool isTTS = false,
        bool ephemeral = false,
        AllowedMentions allowedMentions = null!,
        Embed embed = null!,
        RequestOptions options = null!)
    {
        // todo
        return Task.CompletedTask;
    }
    
    public Conversation(IInteractionContext interactionContext) 
    : base(interactionContext)
    {
    }
}
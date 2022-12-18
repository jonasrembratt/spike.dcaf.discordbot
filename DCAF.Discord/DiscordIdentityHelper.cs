using Discord;
using Discord.WebSocket;
using TetraPak.XP.StringValues;

namespace DCAF.Discord;

public static class DiscordIdentityHelper
{
    public static bool IsIdentity(this IUser user, string identity)
    {
        if (ulong.TryParse(identity, out var id) && user.Id == id)
            return true;

        return MultiStringValue.TryParse<DiscordName>(identity, out var discordName) 
               && user.Username == discordName!.Name && user.Discriminator == discordName.Discriminator;
    }
    
    public static bool IsIdentity(this SocketRole role, string identity)
    {
        if (ulong.TryParse(identity, out var id))
            return role.Id == id;

        return MultiStringValue.TryParse<DiscordName>(identity, out var discordName) 
               && role.Name == discordName!.Name; // && role. Discriminator == discordName.Discriminator;
    }
}
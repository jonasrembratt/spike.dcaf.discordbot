using System;
using DCAF.DiscordBot._lib;

namespace DCAF.DiscordBot.Model
{
    public static class MemberStatusHelper
    {
        public static bool TryParseMemberStatus(this string s, out MemberStatus? status)
        {
            var ident = s.ToIdentifier(IdentCasing.Pascal);
            if (Enum.TryParse(typeof(MemberStatus), s, true, out var e))
            {
                status = (MemberStatus) e!;
                return true;
            }

            status = null;
            return false;
        }
    }
}
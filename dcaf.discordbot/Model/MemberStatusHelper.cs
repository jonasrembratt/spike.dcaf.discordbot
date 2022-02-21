using System;
using DCAF.DiscordBot._lib;

namespace DCAF.DiscordBot.Model
{
    public static class MemberStatusHelper
    {
        public static bool TryParseMemberStatus(this string s, out MemberStatus? status)
        {
            try
            {
                var ident = s.ToIdentifier(IdentCasing.Pascal);
                if (Enum.TryParse(typeof(MemberStatus), ident, true, out var e))
                {
                    status = (MemberStatus) e!;
                    return true;
                }

                status = null;
                return false;
            }
            catch (Exception exception) // nisse
            {
                Console.WriteLine(exception);
                throw;
            }
        }
    }
}
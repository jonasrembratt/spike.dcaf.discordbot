using System;
using DCAF._lib;

namespace DCAF.Model
{
    public static class MemberStatusHelper
    {
        public static bool TryParseMemberStatus(this string s, out MemberStatus? status)
        {
            var ident = s.ToIdentifier(IdentCasing.Pascal);
            if (Enum.TryParse<MemberStatus>(ident, true, out var e))
            {
                status = (MemberStatus) e!;
                return true;
            }

            status = null;
            return false;
        }
    }
}
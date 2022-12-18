using System;

namespace DCAF.Model;

public enum MemberGrade
{
    Unknown,
    // ReSharper disable InconsistentNaming
    OFC,
    OF1,
    OF2,
    OF3,
    OF4,
    OF5,
    OF6
    // ReSharper restore InconsistentNaming
}

public static class MemberGradeHelper
{
    public static bool TryParseMemberGrade(this string? stringValue, out MemberGrade value)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            value = MemberGrade.Unknown;
            return false;
        }

        var s = stringValue.Replace(" ", "").Replace("-", "");
        return Enum.TryParse(s, out value);
    }
        
}
using System.Threading.Tasks;
using DCAF.Discord.Scheduling;
using TetraPak.XP;

public static class PolicyOutcomeHelper
{
    public static async Task SayNextRuntime(this PolicyOutcomeArgs e)
    {
        const DateTimeDefaultFormatOptions Options =
            DateTimeDefaultFormatOptions.ForceUtc |
            DateTimeDefaultFormatOptions.OmitTimeQualifier;
        if (!e.NextRunTime.HasValue)
            return;

        await e.SendMessageAsync($"Policy will be run again {e.NextRunTime.Value.ToStandardString(Options)}");
    }
}
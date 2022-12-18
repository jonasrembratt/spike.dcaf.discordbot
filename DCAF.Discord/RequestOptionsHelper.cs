using System;
using System.Threading.Tasks;
using Discord;

namespace DCAF.Discord;

public static class RequestOptionsHelper
{
    public static RequestOptions WithTimeout(this RequestOptions options, TimeSpan? timeout)
    {
        if (!timeout.HasValue)
            return options;
        
        options.Timeout = (int?)timeout.Value.TotalSeconds;
        return options;
    }
    
    public static RequestOptions WithDefaultRateLimitHandler(this RequestOptions options, TimeSpan? timeout = null)
    {
        if (timeout.HasValue)
        {
            options.WithTimeout(timeout.Value);
        }
    
        options.RatelimitCallback = onRateLimit;
        return options;
    }
    
    static async Task onRateLimit(IRateLimitInfo arg)
    {
        if (arg.ResetAfter.HasValue)
        {
            await Task.Delay(arg.ResetAfter!.Value);
            return;
        }

        if (arg.RetryAfter.HasValue)
        {
            await Task.Delay(TimeSpan.FromSeconds(arg.RetryAfter.Value));
        }
    }

}
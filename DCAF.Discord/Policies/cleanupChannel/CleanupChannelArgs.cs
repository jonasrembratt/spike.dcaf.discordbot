using System;
using TetraPak.XP;
using TetraPak.XP.Configuration;

namespace DCAF.Discord.Policies;

public sealed class CleanupChannelArgs : ConfigurationSectionDecorator
{
    public ulong Channel
    {
        get => this.Get<ulong>();
        set => this.Set(value);
    }
    
    public string? Except
    {
        get => this.Get<string?>();
        set => this.Set(value);
    }
    
    public TimeSpan? MinAge 
    {
        get => this.Get<TimeSpan?>();
        set => this.Set(value);
    }

    public TimeSpan? MaxAge 
    {
        get => this.Get<TimeSpan?>();
        set => this.Set(value);
    }

    public bool IncludePinned
    {
        get => this.Get<bool>();
        set => this.Set(value);
    }
    
    public bool Simulate
    {
        get => this.Get<bool>();
        set => this.Set(value);
    }

    public bool Silent
    {
        get => this.Get<bool>();
        set => this.Set(value);
    }

    public TimeFrame TimeFrame
    {
        get
        {
            if (MinAge is null && MaxAge is null)
                return new TimeFrame(DateTime.MinValue, DateTime.Now);

            var now = XpDateTime.Now;
            var from = MaxAge is { } ? now.Subtract(MaxAge.Value) : DateTime.MinValue;
            var to = MinAge is { } ? now.Subtract(MinAge.Value) : now;
            return new TimeFrame(from, to);
        }
    }
    
    public CleanupChannelArgs(ulong channel,
        string? except,
        string? minAge,
        string? maxAge,
        bool includePinned,
        bool simulate, 
        bool silent)
    {
        Channel = channel;
        Except = except;
        MinAge = minAge.IsAssigned() && minAge!.TryParseTimeSpan(TimeUnits.Hours, out var timespan, ignoreCase:true) 
            ? timespan 
            : null;
        MaxAge = maxAge.IsAssigned() && maxAge!.TryParseTimeSpan(TimeUnits.Hours, out timespan, ignoreCase:true) 
            ? timespan 
            : null;
        IncludePinned = includePinned;
        Simulate = simulate;
        Silent = silent;
    }

    public CleanupChannelArgs(ConfigurationSectionDecoratorArgs args)
        : base(args)
    {
    }
}
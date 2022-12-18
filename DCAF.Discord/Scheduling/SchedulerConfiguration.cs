using System;
using System.Collections.Generic;
using TetraPak.XP;
using TetraPak.XP.Configuration;

namespace DCAF.Discord.Scheduling
{
    public sealed class SchedulerConfiguration : ConfigurationSectionDecorator
    {
        public TimeSpan Interval
        {
            get
            {
                if (Math.Abs(TimeAcceleration - 1d) < double.Epsilon)
                    return this.Get(TimeSpan.FromMinutes(1));

                var interval = this.Get(TimeSpan.FromMinutes(1));
                return TimeSpan.FromTicks((long)(interval.Ticks / TimeAcceleration));
            }
        }

        public ulong DiscordOutputChannel => this.Get<ulong>();

        public double TimeAcceleration => DateTimeSource.Current is XpDateTime xpDateTime ? xpDateTime.TimeAcceleration : 1d;

        public IEnumerable<ScheduledActionConfiguration> Actions { get; }

        List<ScheduledActionConfiguration> loadEntries()
        {
            var subSections = this.GetSubSections();
            var entries = new List<ScheduledActionConfiguration>();
            foreach (var subSection in subSections)
            {
                if (subSection.Key == nameof(DiscordOutputChannel))
                    continue;
                    
                var entry = new ScheduledActionConfiguration(
                    ConfigurationSectionDecoratorArgs.ForSubSection(this, subSection.Key));
                entries.Add(entry);
            }

            return entries;
        }

        public SchedulerConfiguration(ConfigurationSectionDecoratorArgs args) 
        : base(args)
        {
            Actions = loadEntries();
        }
    }
}
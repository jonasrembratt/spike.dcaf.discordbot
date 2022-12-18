using DCAF.Discord.Scheduling;
using TetraPak.XP;
using TetraPak.XP.Configuration;

namespace DCAF.Discord
{
    public sealed class DcafConfiguration : ConfigurationSectionDecorator
    {
        public const string SectionKey = "DCAF";
        
        public ulong GuildId => this.Get<ulong>();

        public DiscordName? BotName
        {
            get
            {
                var name = this.Get<string?>();
                return name.IsUnassigned() 
                    ? null 
                    : new DiscordName(name!);
            }
        }

        public PersonnelSheetConfiguration PersonnelSheet 
        {
            get
            {
                var args = ConfigurationSectionDecoratorArgs.ForSubSection(this, nameof(PersonnelSheet));
                return new PersonnelSheetConfiguration(args);
            }
        }

        public MemberApplicationSheetConfiguration MemberApplicationSheet
        {
            get
            {
                var args = ConfigurationSectionDecoratorArgs.ForSubSection(this, nameof(MemberApplicationSheet));
                return new MemberApplicationSheetConfiguration(args);
            }
        }

        public EventsConfiguration Events
        {
            get
            {
                var args = ConfigurationSectionDecoratorArgs.ForSubSection(this, nameof(Events));
                return new EventsConfiguration(args);
            }
        }

        public SchedulerConfiguration Scheduler
        {
            get
            {
                var args = ConfigurationSectionDecoratorArgs.ForSubSection(this, nameof(Scheduler));
                return new SchedulerConfiguration(args);
            }
        }

        public DcafConfiguration(ConfigurationSectionDecoratorArgs args) 
        : base(args)
        {
        }
    }
}
using System;
using System.Linq;
using TetraPak.XP.Configuration;

namespace DCAF.Discord.Policies
{
    public sealed class MemberStatusPolicyArgs : ConfigurationSectionDecorator
    {
        public MemberStatusPolicyRules[] PolicyConfigurations { get; set; }
        
        public MemberStatusPolicyArgs(ConfigurationSectionDecoratorArgs args)
        : base(args)
        {
            PolicyConfigurations = Section!
                .GetSubSections()
                .Select(subSection
                    => ConfigurationSectionDecoratorArgs.ForSubSection(this, subSection.Key))
                        .Select(e => new MemberStatusPolicyRules(e)).ToArray();
        }
    }

    public sealed class MemberStatusPolicyRules : ConfigurationSectionDecorator
    {
#pragma warning disable CS0169
        string? _criteria;
        string? _setStatus;
        TimeSpan? _allowedAbsence;
        TimeSpan? _rsvpTimeSpan;
#pragma warning restore CS0169
        
        public string Name => Section!.Key;

        public string? Criteria => this.GetFromFieldThenSection<string>();

        public string? SetStatus => this.GetFromFieldThenSection<string>();

        public TimeSpan AllowedAbsence => this.GetFromFieldThenSection<TimeSpan>();
        
        public TimeSpan RsvpTimeSpan => this.GetFromFieldThenSection<TimeSpan>();
        
        public MemberStatusPolicyRules(ConfigurationSectionDecoratorArgs args)
        : base(args)
        {
        }
    }
}
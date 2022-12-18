using TetraPak.XP.Configuration;

namespace DCAF.Discord;

public sealed class MemberApplicationSheetConfiguration : ConfigurationSectionDecorator
{
    public string? SheetName => this.Get<string?>();

    public string? ApplicationName => this.Get<string?>();

    public string? DocumentId => this.Get<string?>();
        
    public MemberApplicationSheetConfiguration(ConfigurationSectionDecoratorArgs args) 
        : base(args)
    {
    }   
}
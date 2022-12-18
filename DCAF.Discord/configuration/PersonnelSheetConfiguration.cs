using TetraPak.XP.Configuration;

namespace DCAF.Discord
{
    public sealed class PersonnelSheetConfiguration : ConfigurationSectionDecorator
    {
        public string? SheetName => this.Get<string?>();

        public string? ApplicationName => this.Get<string?>();

        public string? DocumentId => this.Get<string?>();
        
        public PersonnelSheetConfiguration(ConfigurationSectionDecoratorArgs args) 
        : base(args)
        {
        }
    }
}
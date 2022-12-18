using Microsoft.Extensions.Configuration;
using TetraPak.XP.Configuration;

namespace DCAF;

public static class ConfigurationHelper
{
    public static T WithOverrides<T>(this ConfigurationSectionDecorator config, IConfiguration overrides) 
    where T : ConfigurationSectionDecorator
    {
        var cloned = config.Clone<T>(true);
        foreach (var child in overrides.GetChildren())
        {
            cloned.SetNamed(child.Key, child.Value);
        }
        return (T) cloned;
    }
}
using System.ComponentModel;

namespace Atc.CodingRules.Updater.CLI.Commands.DescriptionAttributes;

[AttributeUsage(AttributeTargets.All)]
public sealed class ProviderCollectingModeDescriptionAttribute : DescriptionAttribute
{
    public override string Description
        => "Sets the CollectingMode. Valid values are: " + string.Join(", ", Enum.GetNames(typeof(ProviderCollectingMode)));
}
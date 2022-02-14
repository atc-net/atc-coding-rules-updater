using System.ComponentModel;

namespace Atc.CodingRules.Updater.CLI.Commands.DescriptionAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate)]
public sealed class ProviderCollectingModeDescriptionAttribute : DescriptionAttribute
{
    public override string Description
    {
        get
        {
            var values = new List<string>
            {
                ProviderCollectingMode.GitHub.ToString(),
                ProviderCollectingMode.ReCollect.ToString(),
            };

            return "Update rules. Valid values are: " + string.Join(", ", values);
        }
    }
}
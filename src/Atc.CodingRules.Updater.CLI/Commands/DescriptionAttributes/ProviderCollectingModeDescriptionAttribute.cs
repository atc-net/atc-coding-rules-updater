using System.ComponentModel;

namespace Atc.CodingRules.Updater.CLI.Commands.DescriptionAttributes;

[AttributeUsage(AttributeTargets.All)]
public sealed class ProviderCollectingModeDescriptionAttribute : DescriptionAttribute
{
    public override string Description
    {
        get
        {
            var defaultValue = new Options().AnalyzerProviderCollectingMode;
            var values = Enum.GetNames(typeof(ProviderCollectingMode))
                .Select(enumValue => enumValue.Equals(defaultValue.ToString(), StringComparison.Ordinal)
                    ? $"{enumValue} (default)"
                    : enumValue)
                .ToList();

            return "Sets the CollectingMode. Valid values are: " +
                   string.Join(", ", values);
        }
    }
}
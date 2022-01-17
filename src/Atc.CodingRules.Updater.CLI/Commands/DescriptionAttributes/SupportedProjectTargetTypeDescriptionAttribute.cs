using System.ComponentModel;

namespace Atc.CodingRules.Updater.CLI.Commands.DescriptionAttributes;

[AttributeUsage(AttributeTargets.All)]
public sealed class SupportedProjectTargetTypeDescriptionAttribute : DescriptionAttribute
{
    public override string Description
    {
        get
        {
            var defaultValue = new Options().ProjectTarget;
            var values = Enum.GetNames(typeof(SupportedProjectTargetType))
                .Select(enumValue => enumValue.Equals(defaultValue.ToString(), StringComparison.Ordinal)
                    ? $"{enumValue} (default)"
                    : enumValue)
                .ToList();

            return "Sets the ProjectTarget. Valid values are: " +
                   string.Join(", ", values);
        }
    }
}
using System.ComponentModel;

namespace Atc.CodingRules.Updater.CLI.Commands.DescriptionAttributes;

[AttributeUsage(AttributeTargets.All)]
public sealed class SupportedSolutionTargetTypeDescriptionAttribute : DescriptionAttribute
{
    public override string Description
    {
        get
        {
            var defaultValue = new Options().SolutionTarget;
            var values = Enum.GetNames(typeof(SupportedSolutionTargetType))
                .Select(enumValue => enumValue.Equals(defaultValue.ToString(), StringComparison.Ordinal)
                    ? $"{enumValue} (default)"
                    : enumValue)
                .ToList();

            return "Sets the SolutionTarget. Valid values are: " +
                   string.Join(", ", values);
        }
    }
}
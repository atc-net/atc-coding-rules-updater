namespace Atc.CodingRules.Updater.CLI.Commands.DescriptionAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate)]
public sealed class SupportedProjectTargetTypeDescriptionAttribute : DescriptionAttribute
{
    public override string Description
    {
        get
        {
            var defaultValue = new OptionsFile().ProjectTarget;
            var values = Enum.GetNames<SupportedProjectTargetType>()
                .Select(enumValue => enumValue.Equals(defaultValue.ToString(), StringComparison.Ordinal)
                    ? $"{enumValue} (default)"
                    : enumValue)
                .ToList();

            return "Sets the ProjectTarget. Valid values are: " +
                   string.Join(", ", values);
        }
    }
}
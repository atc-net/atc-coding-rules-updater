namespace Atc.CodingRules.Updater.CLI.Commands.Settings
{
    public class ProjectCommandSettings : BaseCommandSettings
    {
        [CommandOption("-t|--projectTarget [PROJECTTARGET]")]
        [SupportedProjectTargetTypeDescription]
        public FlagValue<SupportedProjectTargetType> ProjectTarget { get; init; } = new FlagValue<SupportedProjectTargetType>();

        internal static SupportedProjectTargetType? GetProjectTarget(
            ProjectCommandSettings settings)
            => settings.ProjectTarget.IsSet
                ? settings.ProjectTarget.Value
                : null;
    }
}
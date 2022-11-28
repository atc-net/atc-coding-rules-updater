// ReSharper disable StringLiteralTypo
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class ProjectCommandSettings : ProjectBaseCommandSettings
{
    [CommandOption($"{ArgumentCommandConstants.ShortProjectTarget}|{ArgumentCommandConstants.LongProjectTarget} [PROJECTTARGET]")]
    [SupportedProjectTargetTypeDescription]
    public FlagValue<SupportedProjectTargetType> ProjectTarget { get; init; } = new();

    internal static SupportedProjectTargetType? GetProjectTarget(
        ProjectCommandSettings settings)
        => settings.ProjectTarget.IsSet
            ? settings.ProjectTarget.Value
            : null;
}
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

/// <summary>
/// Settings shared by the <c>cpm</c> commands. Used directly by <c>cpm validate</c>.
/// </summary>
/// <remarks>
/// Derives from <see cref="ProjectBaseCommandSettings"/>, not <see cref="ProjectCommandSettings"/>:
/// the cpm commands do not use a project target, so they do not offer <c>--project-target</c>.
/// </remarks>
public class CpmCommandSettings : ProjectBaseCommandSettings
{
    [CommandOption(ArgumentCommandConstants.LongUnifyVersions)]
    [Description("Give every project the highest version of a package instead of keeping differences through VersionOverride. This changes what restores. (default false)")]
    public bool? UnifyVersions { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongBuildFile} [BUILDFILE]")]
    [Description("Solution (.sln/.slnx) or project file to restore. Required when the project path holds more than one.")]
    public FlagValue<string>? BuildFile { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongBuildConfiguration} [BUILDCONFIGURATION]")]
    [Description("Configuration to restore with. Release turns NuGet warnings into errors, as CI does. (default Release)")]
    public FlagValue<string>? BuildConfiguration { get; init; }

    [CommandOption(ArgumentCommandConstants.LongOutputJson)]
    [Description("Emit a machine-readable JSON result on stdout instead of log output.")]
    public bool? OutputJson { get; init; }
}
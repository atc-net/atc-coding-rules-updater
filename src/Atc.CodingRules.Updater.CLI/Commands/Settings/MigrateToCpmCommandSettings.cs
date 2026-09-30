namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

/// <summary>
/// Settings for the <c>migrate-to-cpm</c> command.
/// </summary>
public class MigrateToCpmCommandSettings : ProjectCommandSettings
{
    [CommandOption(ArgumentCommandConstants.LongDryRun)]
    [Description("Show the migration plan without writing any files. (default false)")]
    public bool? DryRun { get; init; }

    [CommandOption(ArgumentCommandConstants.LongUnifyVersions)]
    [Description("Give every project the highest version of a package instead of keeping differences through VersionOverride. This changes what restores. (default false)")]
    public bool? UnifyVersions { get; init; }

    [CommandOption(ArgumentCommandConstants.LongSkipRestoreVerification)]
    [Description("Do not run dotnet restore before and after to verify the migration. Not recommended. (default false)")]
    public bool? SkipRestoreVerification { get; init; }

    [CommandOption(ArgumentCommandConstants.LongAllowDirty)]
    [Description("Allow a dirty or missing git working tree. Without git there is no simple way to undo the migration. (default false)")]
    public bool? AllowDirty { get; init; }

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
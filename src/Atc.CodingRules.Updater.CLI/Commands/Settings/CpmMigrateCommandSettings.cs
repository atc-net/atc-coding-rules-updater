namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

/// <summary>
/// Settings for the <c>cpm migrate</c> command.
/// </summary>
public class CpmMigrateCommandSettings : CpmCommandSettings
{
    [CommandOption(ArgumentCommandConstants.LongDryRun)]
    [Description("Show the migration plan without writing any files. (default false)")]
    public bool? DryRun { get; init; }

    [CommandOption(ArgumentCommandConstants.LongSkipRestoreVerification)]
    [Description("Do not run dotnet restore before and after to verify the migration. Not recommended. (default false)")]
    public bool? SkipRestoreVerification { get; init; }

    [CommandOption(ArgumentCommandConstants.LongAllowDirty)]
    [Description("Allow a dirty or missing git working tree. Without git there is no simple way to undo the migration. (default false)")]
    public bool? AllowDirty { get; init; }
}
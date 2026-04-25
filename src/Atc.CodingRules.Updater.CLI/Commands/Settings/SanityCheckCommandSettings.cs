namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

/// <summary>
/// Settings for the <c>sanity-check</c> command.
/// </summary>
public class SanityCheckCommandSettings : ProjectCommandSettings
{
    /// <summary>When set, the command emits a JSON summary on stdout instead of human-readable log output.</summary>
    [CommandOption(ArgumentCommandConstants.LongOutputJson)]
    [Description("Emit a machine-readable JSON summary on stdout (Severity / Code / Message / FilePath per diagnostic). Useful for CI.")]
    public bool? OutputJson { get; init; }
}
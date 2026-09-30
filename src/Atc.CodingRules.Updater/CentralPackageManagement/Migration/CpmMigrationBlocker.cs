namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// Something that stops a repository from being migrated to Central Package Management until it
/// is fixed by hand.
/// </summary>
/// <param name="Code">Stable identifier, for example <c>FloatingVersion</c>.</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="FilePath">The file the blocker was found in, when there is one.</param>
public sealed record CpmMigrationBlocker(
    string Code,
    string Message,
    string? FilePath);
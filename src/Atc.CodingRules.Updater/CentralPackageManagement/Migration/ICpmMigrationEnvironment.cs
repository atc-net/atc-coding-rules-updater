namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// The outside world the migration depends on: git and <c>dotnet restore</c>.
/// </summary>
public interface ICpmMigrationEnvironment
{
    /// <summary>Returns <c>true</c> for a clean working tree, <c>false</c> for a dirty one, and <c>null</c> when <paramref name="projectPath"/> is not in a git repository.</summary>
    Task<bool?> IsWorkingTreeCleanAsync(
        DirectoryInfo projectPath,
        CancellationToken cancellationToken);

    /// <summary>Restores <paramref name="restoreTarget"/> (or the repository root) and captures the result.</summary>
    Task<RestoreSnapshot> RestoreAsync(
        DirectoryInfo projectPath,
        string? restoreTarget,
        string configuration,
        CancellationToken cancellationToken);
}
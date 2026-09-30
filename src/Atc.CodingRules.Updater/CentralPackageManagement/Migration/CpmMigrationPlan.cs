namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// What migrating a repository to Central Package Management would do, produced by
/// <see cref="CpmMigrationPlanner.Plan(DirectoryInfo, bool)"/> without writing anything.
/// </summary>
public sealed class CpmMigrationPlan
{
    /// <summary><c>true</c> when the repository already uses Central Package Management.</summary>
    public bool AlreadyMigrated { get; init; }

    /// <summary>Problems that must be fixed by hand first. When there are any, <see cref="FileChanges"/> is empty.</summary>
    public IReadOnlyList<CpmMigrationBlocker> Blockers { get; init; } = [];

    /// <summary>The packages declared in the new <c>Directory.Packages.props</c>.</summary>
    public IReadOnlyList<CpmMigrationPackage> Packages { get; init; } = [];

    /// <summary>References that keep a different version through <c>VersionOverride</c>.</summary>
    public IReadOnlyList<CpmMigrationOverride> Overrides { get; init; } = [];

    /// <summary>New content per absolute file path, including the new <c>Directory.Packages.props</c>.</summary>
    public IReadOnlyDictionary<string, string> FileChanges { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary><c>true</c> when there is something to migrate and nothing blocks it.</summary>
    public bool CanMigrate
        => !AlreadyMigrated && Blockers.Count == 0 && FileChanges.Count > 0;
}
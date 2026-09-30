namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// How a <c>migrate-to-cpm</c> run ended.
/// </summary>
public enum CpmMigrationOutcome
{
    /// <summary>The repository already uses Central Package Management; nothing was done.</summary>
    AlreadyMigrated,

    /// <summary>There were no package references to migrate.</summary>
    NothingToMigrate,

    /// <summary>Something must be fixed by hand first; nothing was written.</summary>
    Blocked,

    /// <summary>The plan was shown; nothing was written.</summary>
    DryRun,

    /// <summary>The files were written and restore verified them.</summary>
    Migrated,

    /// <summary>The files were written, restore showed a difference, and every file was put back.</summary>
    RolledBack,
}
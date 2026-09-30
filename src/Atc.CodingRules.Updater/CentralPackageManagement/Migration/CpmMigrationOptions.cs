namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// Options for <see cref="CpmMigrationExecutor"/>.
/// </summary>
public sealed class CpmMigrationOptions
{
    /// <summary>Show the plan without writing anything.</summary>
    public bool DryRun { get; init; }

    /// <summary>Give every project the highest version instead of keeping differences through <c>VersionOverride</c>. This changes what restores.</summary>
    public bool UnifyVersions { get; init; }

    /// <summary>Skip the before/after restore comparison. Not recommended.</summary>
    public bool SkipRestoreVerification { get; init; }

    /// <summary>Allow a dirty or missing git working tree.</summary>
    public bool AllowDirty { get; init; }

    /// <summary>The solution or project to restore; <c>null</c> restores the repository root.</summary>
    public string? RestoreTarget { get; init; }

    /// <summary>The configuration to restore with. Release by default, where warnings are errors.</summary>
    public string BuildConfiguration { get; init; } = "Release";
}
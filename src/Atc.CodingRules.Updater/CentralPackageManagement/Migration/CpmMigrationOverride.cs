namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// A reference that keeps its own version through <c>VersionOverride</c>, because it differs from
/// the central version and the migration preserves what each project restores.
/// </summary>
/// <param name="FilePath">The file holding the reference.</param>
/// <param name="Id">The package id.</param>
/// <param name="Version">The version the reference keeps.</param>
public sealed record CpmMigrationOverride(
    string FilePath,
    string Id,
    string Version);
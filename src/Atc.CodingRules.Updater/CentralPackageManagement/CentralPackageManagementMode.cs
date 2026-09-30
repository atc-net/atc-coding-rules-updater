namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// How the updater decides whether a repository uses NuGet Central Package Management (CPM).
/// </summary>
public enum CentralPackageManagementMode
{
    /// <summary>Detect CPM from <c>ManagePackageVersionsCentrally</c> in the root props files.</summary>
    Auto,

    /// <summary>Always use the non-CPM behaviour, even when CPM is detected. An escape hatch.</summary>
    Disabled,
}
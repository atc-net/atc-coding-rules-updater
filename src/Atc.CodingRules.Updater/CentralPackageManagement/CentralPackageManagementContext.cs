namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// What <see cref="CentralPackageManagementHelper.Detect"/> found about Central Package Management
/// (CPM) in a repository.
/// </summary>
public sealed class CentralPackageManagementContext
{
    public CentralPackageManagementContext(
        bool isEnabled,
        FileInfo? packagesPropsFile,
        IReadOnlyList<PackageItem> entries,
        bool hasPackagesPropsWithoutFlag,
        bool supportsGlobalPackageReference)
    {
        IsEnabled = isEnabled;
        PackagesPropsFile = packagesPropsFile;
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        HasPackagesPropsWithoutFlag = hasPackagesPropsWithoutFlag;
        SupportsGlobalPackageReference = supportsGlobalPackageReference;
    }

    /// <summary>A context for a repository that does not use CPM.</summary>
    public static CentralPackageManagementContext NotEnabled { get; } = new(
        isEnabled: false,
        packagesPropsFile: null,
        entries: [],
        hasPackagesPropsWithoutFlag: false,
        supportsGlobalPackageReference: true);

    /// <summary><c>true</c> when the repository uses CPM and the CPM-aware path should run.</summary>
    public bool IsEnabled { get; }

    /// <summary>The root <c>Directory.Packages.props</c>, or <c>null</c> when it does not exist.</summary>
    public FileInfo? PackagesPropsFile { get; }

    /// <summary>The <c>PackageVersion</c> and <c>GlobalPackageReference</c> items in <see cref="PackagesPropsFile"/>.</summary>
    public IReadOnlyList<PackageItem> Entries { get; }

    /// <summary>
    /// <c>true</c> when a root <c>Directory.Packages.props</c> exists but CPM is not switched on.
    /// The updater then treats the repository as non-CPM and never flips the flag itself.
    /// </summary>
    public bool HasPackagesPropsWithoutFlag { get; }

    /// <summary>
    /// <c>false</c> when the SDK pinned in <c>global.json</c> is older than 7.0.100, which is the
    /// first SDK that understands <c>GlobalPackageReference</c>.
    /// </summary>
    public bool SupportsGlobalPackageReference { get; }

    /// <summary>Returns <c>true</c> when <paramref name="packageId"/> has a <c>PackageVersion</c> or <c>GlobalPackageReference</c>.</summary>
    public bool HasEntry(string packageId)
        => Entries.Any(x => x.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
}
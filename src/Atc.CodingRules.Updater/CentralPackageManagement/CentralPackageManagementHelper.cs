namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// Detects NuGet Central Package Management (CPM) in a repository.
/// </summary>
/// <remarks>
/// Only the root of the repository is inspected. Nested or imported <c>Directory.Packages.props</c>
/// files are not followed.
/// </remarks>
public static class CentralPackageManagementHelper
{
    /// <summary>The file NuGet reads centrally managed package versions from.</summary>
    public const string PackagesPropsFileName = "Directory.Packages.props";

    private const string FlagPropertyName = "ManagePackageVersionsCentrally";

    /// <summary>The first SDK that understands <c>GlobalPackageReference</c> (it shipped in 7.0.100-preview7).</summary>
    private static readonly Version GlobalPackageReferenceMinimumSdk = new(7, 0, 100);

    /// <summary>
    /// Inspects the root of <paramref name="projectPath"/> and reports whether CPM is on.
    /// </summary>
    /// <remarks>
    /// CPM is on when <c>ManagePackageVersionsCentrally</c> is <c>true</c> in the root
    /// <c>Directory.Packages.props</c> or the root <c>Directory.Build.props</c>; both are legal
    /// places for the property.
    /// </remarks>
    public static CentralPackageManagementContext Detect(
        DirectoryInfo projectPath,
        CentralPackageManagementMode mode = CentralPackageManagementMode.Auto)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        if (mode == CentralPackageManagementMode.Disabled)
        {
            return CentralPackageManagementContext.NotEnabled;
        }

        var packagesPropsFile = new FileInfo(Path.Combine(projectPath.FullName, PackagesPropsFileName));
        var packagesPropsContent = packagesPropsFile.Exists
            ? FileHelper.ReadAllText(packagesPropsFile)
            : null;

        var buildPropsFile = new FileInfo(Path.Combine(projectPath.FullName, DirectoryBuildPropsHelper.FileName));
        var buildPropsContent = buildPropsFile.Exists
            ? FileHelper.ReadAllText(buildPropsFile)
            : null;

        var isEnabled = HasFlag(packagesPropsContent) || HasFlag(buildPropsContent);
        if (!isEnabled)
        {
            return new CentralPackageManagementContext(
                isEnabled: false,
                packagesPropsFile: null,
                entries: [],
                hasPackagesPropsWithoutFlag: packagesPropsFile.Exists,
                supportsGlobalPackageReference: true);
        }

        var entries = packagesPropsContent is null
            ? []
            : (PackageItemParser.Parse(packagesPropsContent) ?? [])
                .Where(x => x.ItemType != PackageItemType.PackageReference)
                .ToList();

        return new CentralPackageManagementContext(
            isEnabled: true,
            packagesPropsFile.Exists ? packagesPropsFile : null,
            entries,
            hasPackagesPropsWithoutFlag: false,
            SupportsGlobalPackageReference(projectPath));
    }

    private static bool HasFlag(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            return XDocument
                .Parse(content)
                .Descendants()
                .Any(x => x.Name.LocalName.Equals(FlagPropertyName, StringComparison.Ordinal) &&
                          x.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase));
        }
        catch (XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the SDK pinned in <c>global.json</c>. Without a pin the running SDK builds, and this
    /// tool itself needs a far newer one than 7.0.100, so support is assumed.
    /// </summary>
    internal static bool SupportsGlobalPackageReference(
        DirectoryInfo projectPath)
    {
        var globalJsonFile = new FileInfo(Path.Combine(projectPath.FullName, "global.json"));
        if (!globalJsonFile.Exists)
        {
            return true;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(
                FileHelper.ReadAllText(globalJsonFile),
                new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });

            if (!document.RootElement.TryGetProperty("sdk", out var sdk) ||
                !sdk.TryGetProperty("version", out var versionElement))
            {
                return true;
            }

            var versionText = versionElement.GetString() ?? string.Empty;
            var dashIndex = versionText.IndexOf('-', StringComparison.Ordinal);
            if (dashIndex >= 0)
            {
                versionText = versionText[..dashIndex];
            }

            return !Version.TryParse(versionText, out var version) ||
                   version >= GlobalPackageReferenceMinimumSdk;
        }
        catch (System.Text.Json.JsonException)
        {
            return true;
        }
    }
}
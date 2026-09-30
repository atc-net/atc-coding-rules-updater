namespace Atc.CodingRules.Updater.MsBuild;

/// <summary>
/// The MSBuild item types that carry NuGet package identities.
/// </summary>
public enum PackageItemType
{
    /// <summary>A <c>&lt;PackageReference&gt;</c> in a project or props file.</summary>
    PackageReference,

    /// <summary>A <c>&lt;PackageVersion&gt;</c> in <c>Directory.Packages.props</c> (Central Package Management).</summary>
    PackageVersion,

    /// <summary>A <c>&lt;GlobalPackageReference&gt;</c> in <c>Directory.Packages.props</c>, applied to every project.</summary>
    GlobalPackageReference,
}
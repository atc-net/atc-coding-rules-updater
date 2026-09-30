namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// A package entry that <c>Directory.Packages.props</c> must contain for translated props to restore.
/// </summary>
/// <param name="ItemType"><see cref="PackageItemType.GlobalPackageReference"/> or <see cref="PackageItemType.PackageVersion"/>.</param>
/// <param name="Id">The package id.</param>
/// <param name="Version">The version to declare.</param>
/// <param name="GroupLabel">The <c>ItemGroup</c> label new entries are appended under.</param>
public sealed record CpmRequiredEntry(
    PackageItemType ItemType,
    string Id,
    string Version,
    string GroupLabel);
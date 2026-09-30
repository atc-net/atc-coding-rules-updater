namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// One package the migration declares in the new <c>Directory.Packages.props</c>.
/// </summary>
/// <param name="Id">The package id.</param>
/// <param name="Version">The central version.</param>
/// <param name="ItemType"><see cref="PackageItemType.PackageVersion"/> or <see cref="PackageItemType.GlobalPackageReference"/>.</param>
public sealed record CpmMigrationPackage(
    string Id,
    string Version,
    PackageItemType ItemType);
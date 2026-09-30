namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// The result of <see cref="DirectoryPackagesPropsHelper.Apply"/>.
/// </summary>
/// <param name="Content">The new file content; equal to the input when nothing changed.</param>
/// <param name="Bumps">Entries whose version was raised.</param>
/// <param name="Appended">Required entries that were missing and have been added.</param>
public sealed record DirectoryPackagesPropsUpdate(
    string Content,
    IReadOnlyList<DotnetNugetPackage> Bumps,
    IReadOnlyList<CpmRequiredEntry> Appended);
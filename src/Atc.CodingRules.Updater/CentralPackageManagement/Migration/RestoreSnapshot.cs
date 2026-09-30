namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// What one <c>dotnet restore</c> produced: its exit code, the NuGet warnings it raised, and the
/// packages resolved for every project and target framework.
/// </summary>
/// <param name="ExitCode">The restore exit code.</param>
/// <param name="Warnings">Each distinct warning as <c>NUxxxx: message</c>, without machine-specific paths.</param>
/// <param name="Packages">Per <c>project.assets.json</c> (relative path), the resolved <c>tfm|id/version</c> entries.</param>
public sealed record RestoreSnapshot(
    int ExitCode,
    IReadOnlySet<string> Warnings,
    IReadOnlyDictionary<string, IReadOnlySet<string>> Packages);
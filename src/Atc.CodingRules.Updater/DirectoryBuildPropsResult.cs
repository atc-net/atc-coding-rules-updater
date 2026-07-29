namespace Atc.CodingRules.Updater;

/// <summary>
/// Result of handling one <c>Directory.Build.props</c>: what happened to the file, which package
/// versions were bumped, and how the local file differs from the distribution.
/// </summary>
/// <param name="Outcome">What happened to the file.</param>
/// <param name="PackageBumps">Package references whose version was raised.</param>
/// <param name="Drift">Presence differences against the distribution, which are reported but never applied.</param>
public sealed record DirectoryBuildPropsResult(
    FileUpdateOutcome Outcome,
    IReadOnlyList<DotnetNugetPackage> PackageBumps,
    DirectoryBuildPropsDrift Drift)
{
    /// <summary>A result carrying nothing but an outcome.</summary>
    public static DirectoryBuildPropsResult From(FileUpdateOutcome outcome)
        => new(outcome, [], DirectoryBuildPropsDrift.Empty);
}
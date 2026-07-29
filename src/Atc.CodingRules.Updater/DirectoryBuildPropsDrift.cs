namespace Atc.CodingRules.Updater;

/// <summary>
/// Difference in <em>presence</em> between the upstream <c>Directory.Build.props</c> and the
/// local one.
/// </summary>
/// <remarks>
/// Only presence is compared, never values. An existing local props file is deliberately never
/// overwritten because it carries repository-specific values (<c>OrganizationName</c>,
/// <c>RepositoryName</c>, …), and package versions are handled separately by
/// <c>--useLatestMinorNugetVersion</c>. Comparing values would therefore report differences that
/// are correct by design.
/// </remarks>
/// <param name="PackageReferencesOnlyUpstream">Package ids the distribution has that the local file does not — typically a newly added analyzer.</param>
/// <param name="PackageReferencesOnlyLocal">Package ids the local file has that the distribution no longer lists.</param>
/// <param name="PropertiesOnlyUpstream">MSBuild property names the distribution has that the local file does not.</param>
public sealed record DirectoryBuildPropsDrift(
    IReadOnlyList<string> PackageReferencesOnlyUpstream,
    IReadOnlyList<string> PackageReferencesOnlyLocal,
    IReadOnlyList<string> PropertiesOnlyUpstream)
{
    /// <summary>An empty result, used when either side could not be parsed.</summary>
    public static DirectoryBuildPropsDrift Empty { get; } = new([], [], []);

    /// <summary>Total number of differences across all three categories.</summary>
    public int Count
        => PackageReferencesOnlyUpstream.Count +
           PackageReferencesOnlyLocal.Count +
           PropertiesOnlyUpstream.Count;

    /// <summary>Returns <c>true</c> when there is at least one difference to report.</summary>
    public bool HasDrift => Count > 0;
}
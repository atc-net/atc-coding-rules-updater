namespace Atc.CodingRules.Updater.CLI.Models;

/// <summary>How one local <c>Directory.Build.props</c> differs from the distribution.</summary>
/// <param name="Area">Mapping area the props file belongs to.</param>
/// <param name="PackagesOnlyInDistribution">Package ids the distribution has that the local file lacks.</param>
/// <param name="PackagesOnlyInLocalFile">Package ids the local file has that the distribution does not list.</param>
/// <param name="PropertiesOnlyInDistribution">MSBuild property names the distribution has that the local file lacks.</param>
public sealed record RunDriftEntry(
    string Area,
    IReadOnlyList<string> PackagesOnlyInDistribution,
    IReadOnlyList<string> PackagesOnlyInLocalFile,
    IReadOnlyList<string> PropertiesOnlyInDistribution);
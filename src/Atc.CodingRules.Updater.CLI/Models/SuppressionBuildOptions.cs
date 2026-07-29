namespace Atc.CodingRules.Updater.CLI.Models;

/// <summary>
/// How the repeated builds behind <c>--useTemporarySuppressions</c> should be invoked.
/// </summary>
/// <param name="UseReleaseConfiguration">
/// Build in Release when <c>true</c>, Debug otherwise. Release is the default, since that is what
/// the tool has always done — but a Release-only target such as an obfuscator can make the build
/// impossible to complete, which is what <c>--buildConfiguration</c> exists to work around.
/// </param>
/// <param name="AdditionalBuildArguments">
/// Extra arguments appended to <c>dotnet build</c>, already formatted as MSBuild switches.
/// </param>
public sealed record SuppressionBuildOptions(
    bool UseReleaseConfiguration,
    string AdditionalBuildArguments)
{
    /// <summary>Release with no extra arguments — the behaviour before these options existed.</summary>
    public static SuppressionBuildOptions Default { get; } = new(UseReleaseConfiguration: true, string.Empty);
}
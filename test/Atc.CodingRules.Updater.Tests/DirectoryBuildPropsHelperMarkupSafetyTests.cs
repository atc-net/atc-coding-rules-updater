namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Log messages are rendered as Spectre markup by the console sink, so any interpolated value
/// that can contain '[' has to be escaped. A NuGet version range ("[2.9.0,3.0.0)") or an exact
/// pin ("[2.1.4]") does, and an unescaped one threw out of the log call itself — aborting the
/// whole run for that area with "An error occurred while writing to logger(s)".
/// </summary>
/// <remarks>
/// This is deliberately separate from <see cref="DirectoryBuildPropsHelperPackageVersionTests"/>.
/// That class pins that the skip is reported at all, and asserts the message contains the raw
/// version — which escaping necessarily breaks. Keeping the two properties apart lets each stay
/// exact instead of loosening the other.
/// </remarks>
public sealed class DirectoryBuildPropsHelperMarkupSafetyTests
{
    private readonly ITestOutputHelper testOutput;

    public DirectoryBuildPropsHelperMarkupSafetyTests(
        ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Theory]
    [InlineData("PreReleasePackage", "1.2.3-beta")]
    [InlineData("FloatingPackage", "3.0.*")]
    [InlineData("MsBuildPropertyPackage", "$(SomeVersion)")]
    [InlineData("RangePackage", "[2.9.0,3.0.0)")]
    [InlineData("ExactPinPackage", "[2.1.4]")]
    public void GetPackageReferencesThatNeedsToBeUpdated_LogsMarkupSafeMessage_WhenVersionIsNotParseable(
        string packageId,
        string version)
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        var fileContent = $"""
            <Project>
              <ItemGroup>
                <PackageReference Include="{packageId}" Version="{version}" />
              </ItemGroup>
            </Project>
            """;

        DirectoryBuildPropsHelper.GetPackageReferencesThatNeedsToBeUpdated(logger, fileContent);

        var skipMessages = logger.Entries
            .Select(x => x.Message)
            .Where(x => x.Contains("Skipping", StringComparison.Ordinal))
            .ToList();

        skipMessages.Should().ContainSingle();

        // Constructing a Markup is what the console sink does; it throws on a malformed tag.
        var exception = Record.Exception(() => new Spectre.Console.Markup(skipMessages[0]));

        exception.Should().BeNull(
            $"the logged message must be renderable as markup, but was: {skipMessages[0]}");
    }
}
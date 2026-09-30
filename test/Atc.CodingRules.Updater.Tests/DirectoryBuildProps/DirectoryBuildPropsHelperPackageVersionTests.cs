namespace Atc.CodingRules.Updater.Tests.DirectoryBuildProps;

/// <summary>
/// Package references whose version is not a parseable <see cref="Version"/> — prerelease,
/// floating, or an MSBuild property — cannot be compared and are skipped. These tests pin that
/// the skip is reported instead of being silent.
/// </summary>
/// <remarks>
/// Every version used here is unparseable, so no NuGet lookup is performed and the tests stay
/// offline.
/// </remarks>
public sealed class DirectoryBuildPropsHelperPackageVersionTests
{
    private readonly ITestOutputHelper testOutput;

    public DirectoryBuildPropsHelperPackageVersionTests(
        ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Theory]
    [InlineData("PreReleasePackage", "1.2.3-beta")]
    [InlineData("FloatingPackage", "3.0.*")]
    [InlineData("MsBuildPropertyPackage", "$(SomeVersion)")]
    public void GetPackageReferencesThatNeedsToBeUpdated_ReportsSkippedPackage_WhenVersionIsNotParseable(
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

        var actual = DirectoryBuildPropsHelper.GetPackageReferencesThatNeedsToBeUpdated(logger, fileContent);

        actual.Should().BeEmpty();
        logger.Entries
            .Should().Contain(x => x.Message.Contains(packageId, StringComparison.Ordinal)
                                   && x.Message.Contains(version, StringComparison.Ordinal));
    }

    /// <summary>
    /// The distributed props files carry the legacy MSBuild namespace and an XML declaration.
    /// A parser that matches the un-namespaced element name finds nothing in them, and every
    /// bump silently does nothing.
    /// </summary>
    [Fact]
    public void GetPackageReferencesThatNeedsToBeUpdated_SeesPackageReferences_WhenPropsFileHasMsBuildNamespace()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        const string fileContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemGroup Label="Code Analyzers">
                <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" PrivateAssets="All" />
              </ItemGroup>
            </Project>
            """;

        DirectoryBuildPropsHelper.GetPackageReferencesThatNeedsToBeUpdated(logger, fileContent);

        logger.Entries
            .Should().Contain(x => x.Message.Contains("StyleCop.Analyzers", StringComparison.Ordinal));
    }

    [Fact]
    public void GetPackageReferencesThatNeedsToBeUpdated_SeesPackageReferences_WhenVersionIsAChildElement()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        const string fileContent = """
            <Project>
              <ItemGroup>
                <PackageReference Include="ChildVersionPackage">
                  <Version>1.2.3-beta</Version>
                </PackageReference>
              </ItemGroup>
            </Project>
            """;

        DirectoryBuildPropsHelper.GetPackageReferencesThatNeedsToBeUpdated(logger, fileContent);

        logger.Entries
            .Should().Contain(x => x.Message.Contains("ChildVersionPackage", StringComparison.Ordinal));
    }
}
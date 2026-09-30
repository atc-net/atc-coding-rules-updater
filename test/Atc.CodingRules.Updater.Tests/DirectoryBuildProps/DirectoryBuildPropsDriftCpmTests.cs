namespace Atc.CodingRules.Updater.Tests.DirectoryBuildProps;

/// <summary>
/// Under Central Package Management the analyzers live in <c>Directory.Packages.props</c> as
/// <c>GlobalPackageReference</c> items, so a root props without them is not drift.
/// </summary>
public sealed class DirectoryBuildPropsDriftCpmTests
{
    private const string LocalRootWithoutAnalyzers = """
        <Project>
          <PropertyGroup>
            <OrganizationName>KL</OrganizationName>
          </PropertyGroup>
        </Project>
        """;

    [Fact]
    public void GetDrift_DoesNotReportAnalyzers_WhenTheyAreGlobalPackageReferences()
    {
        string[] globalIds = ["Atc.Analyzer", "AsyncFixer", "Meziantou.Analyzer", "StyleCop.Analyzers", "SonarAnalyzer.CSharp"];

        var actual = DirectoryBuildPropsHelper.GetDrift(TestFixtures.DistributionRootProps(), LocalRootWithoutAnalyzers, globalIds);

        actual.PackageReferencesOnlyUpstream.Should().BeEmpty();
    }

    [Fact]
    public void GetDrift_StillReportsAnalyzersThatAreNotDeclaredAnywhere()
    {
        string[] globalIds = ["AsyncFixer"];

        var actual = DirectoryBuildPropsHelper.GetDrift(TestFixtures.DistributionRootProps(), LocalRootWithoutAnalyzers, globalIds);

        actual.PackageReferencesOnlyUpstream.Should().Contain("Meziantou.Analyzer").And.NotContain("AsyncFixer");
    }

    [Fact]
    public void GetDrift_ReportsOnlyRealPackageSetDifferences_ForVersionlessTestProps()
    {
        const string localTest = """
            <Project>
              <ItemGroup>
                <PackageReference Include="Atc.Test" />
                <PackageReference Include="NSubstitute.Analyzers.CSharp" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" />
                <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
                <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
                <PackageReference Include="xunit.v3" />
              </ItemGroup>
            </Project>
            """;

        var actual = DirectoryBuildPropsHelper.GetDrift(TestFixtures.DistributionTestProps(), localTest, []);

        actual.PackageReferencesOnlyUpstream.Should().Equal("xunit.runner.visualstudio", "xunit.v3.mtp-v2");
        actual.PackageReferencesOnlyLocal.Should().Equal("xunit.v3");
    }
}
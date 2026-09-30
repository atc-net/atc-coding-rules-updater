namespace Atc.CodingRules.Updater.Tests.CentralPackageManagement;

/// <summary>
/// Translates the distributed props files into the Central Package Management layout. The
/// fixtures are verbatim copies of the atc-coding-rules dotnet10 distribution.
/// </summary>
public sealed class CpmContentTransformerTests
{
    [Fact]
    public void Transform_Root_LeavesNoVersionedPackageReference()
    {
        var actual = CpmContentTransformer.Transform(TestFixtures.DistributionRootProps(), "root", supportsGlobalPackageReference: true);

        PackageItemParser.Parse(actual.Content)!
            .Should().NotContain(x => x.ItemType == PackageItemType.PackageReference);
    }

    [Fact]
    public void Transform_Root_TurnsCodeAnalyzersIntoGlobalPackageReferences()
    {
        var actual = CpmContentTransformer.Transform(TestFixtures.DistributionRootProps(), "root", supportsGlobalPackageReference: true);

        actual.RequiredEntries.Should().HaveCount(5);
        actual.RequiredEntries.Should().OnlyContain(x =>
            x.ItemType == PackageItemType.GlobalPackageReference &&
            x.GroupLabel == "atc-coding-rules: Code Analyzers");
        actual.RequiredEntries.Should().ContainSingle(x => x.Id == "AsyncFixer" && x.Version == "2.1.0");
        actual.AnalyzersSkipped.Should().BeFalse();
    }

    [Fact]
    public void Transform_Root_LeavesAPointerCommentAndKeepsProperties()
    {
        var upstream = TestFixtures.DistributionRootProps();

        var actual = CpmContentTransformer.Transform(upstream, "root", supportsGlobalPackageReference: true);

        actual.Content.Should().Contain("Directory.Packages.props");
        actual.Content.Should().Contain("<AnalysisLevel>latest-All</AnalysisLevel>");
        actual.Content.Should().Contain("<OrganizationName><!-- insert organization name here --></OrganizationName>");
    }

    [Fact]
    public void Transform_Root_SkipsAnalyzers_WhenSdkDoesNotSupportGlobalPackageReference()
    {
        var actual = CpmContentTransformer.Transform(TestFixtures.DistributionRootProps(), "root", supportsGlobalPackageReference: false);

        actual.AnalyzersSkipped.Should().BeTrue();
        actual.RequiredEntries.Should().BeEmpty();
        PackageItemParser.Parse(actual.Content)!.Should().BeEmpty();
    }

    [Fact]
    public void Transform_Test_MakesReferencesVersionlessAndRequiresPackageVersions()
    {
        var actual = CpmContentTransformer.Transform(TestFixtures.DistributionTestProps(), "test", supportsGlobalPackageReference: true);

        var references = PackageItemParser.Parse(actual.Content)!;
        references.Should().HaveCount(7);
        references.Should().OnlyContain(x => x.ItemType == PackageItemType.PackageReference && x.Version == null);

        actual.RequiredEntries.Should().HaveCount(7);
        actual.RequiredEntries.Should().OnlyContain(x =>
            x.ItemType == PackageItemType.PackageVersion &&
            x.GroupLabel == "atc-coding-rules: test");
        actual.RequiredEntries.Should().ContainSingle(x => x.Id == "Atc.Test" && x.Version == "2.0.16");
    }

    [Fact]
    public void Transform_Test_KeepsMetadataOnReference_AndNeverPromotesItToGlobal()
    {
        // xunit.runner.visualstudio carries PrivateAssets=all. A PrivateAssets-based rule would
        // promote it to a GlobalPackageReference and push a test runner into every src project.
        var actual = CpmContentTransformer.Transform(TestFixtures.DistributionTestProps(), "test", supportsGlobalPackageReference: true);

        actual.Content.Should().Contain("<PrivateAssets>all</PrivateAssets>");
        actual.RequiredEntries.Should().ContainSingle(x => x.Id == "xunit.runner.visualstudio")
            .Which.ItemType.Should().Be(PackageItemType.PackageVersion);
    }

    [Fact]
    public void Transform_ReturnsContentUnchanged_WhenThereAreNoPackageReferences()
    {
        const string upstream = "<Project>\n  <PropertyGroup>\n    <OutputType>Library</OutputType>\n  </PropertyGroup>\n</Project>";

        var actual = CpmContentTransformer.Transform(upstream, "src", supportsGlobalPackageReference: true);

        actual.Content.Should().Be(upstream);
        actual.RequiredEntries.Should().BeEmpty();
    }
}
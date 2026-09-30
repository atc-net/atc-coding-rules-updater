namespace Atc.CodingRules.Updater.Tests.DirectoryBuildProps;

/// <summary>
/// The per-area props handling when the repository uses Central Package Management. The
/// upstream content is passed in, so these tests run against real files but never download.
/// </summary>
public sealed class DirectoryBuildPropsHelperCpmTests
{
    private const string PackagesProps = """
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
          </PropertyGroup>
          <ItemGroup Label="Code Analyzers">
            <GlobalPackageReference Include="Atc.Analyzer" Version="0.1.23" />
            <GlobalPackageReference Include="AsyncFixer" Version="2.1.0" />
            <GlobalPackageReference Include="Meziantou.Analyzer" Version="3.0.50" />
            <GlobalPackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
            <GlobalPackageReference Include="SonarAnalyzer.CSharp" Version="10.23.0.137933" />
          </ItemGroup>
        </Project>
        """;

    private readonly ITestOutputHelper testOutput;

    public DirectoryBuildPropsHelperCpmTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public void HandleFileWithCpm_CreatesVersionlessProps_AndCollectsRequiredEntries()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        using var logger = testOutput.BuildLogger();
        var required = new List<CpmRequiredEntry>();

        var actual = DirectoryBuildPropsHelper.HandleFileWithCentralPackageManagement(
            logger,
            "test",
            TestFixtures.DistributionTestProps(),
            workspace.Directory("test"),
            Detect(workspace),
            required);

        actual.Outcome.Should().Be(FileUpdateOutcome.Created);
        PackageItemParser.Parse(workspace.ReadFile("test/Directory.Build.props"))!
            .Should().OnlyContain(x => x.Version == null);
        required.Should().HaveCount(7).And.OnlyContain(x => x.ItemType == PackageItemType.PackageVersion);
    }

    [Fact]
    public void HandleFileWithCpm_DoesNotWrite_InDryRun_ButReportsTheSameOutcomeAndEntries()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        using var logger = testOutput.BuildLogger();
        var required = new List<CpmRequiredEntry>();

        var actual = DirectoryBuildPropsHelper.HandleFileWithCentralPackageManagement(
            logger,
            "test",
            TestFixtures.DistributionTestProps(),
            workspace.Directory("test"),
            Detect(workspace),
            required,
            dryRun: true);

        actual.Outcome.Should().Be(FileUpdateOutcome.Created);
        workspace.FileExists("test/Directory.Build.props").Should().BeFalse();
        required.Should().HaveCount(7);
    }

    [Fact]
    public void HandleFileWithCpm_ReportsUnchanged_WhenLocalFileMatchesTranslatedContent()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        var translated = CpmContentTransformer.Transform(TestFixtures.DistributionTestProps(), "test", supportsGlobalPackageReference: true);
        workspace.WriteFile("test/Directory.Build.props", translated.Content);
        using var logger = testOutput.BuildLogger();
        var required = new List<CpmRequiredEntry>();

        var actual = DirectoryBuildPropsHelper.HandleFileWithCentralPackageManagement(
            logger,
            "test",
            TestFixtures.DistributionTestProps(),
            workspace.Directory("test"),
            Detect(workspace),
            required);

        actual.Outcome.Should().Be(FileUpdateOutcome.Unchanged);
        required.Should().BeEmpty();
    }

    [Fact]
    public void HandleFileWithCpm_NeverModifiesAnExistingFile_AndReportsNoAnalyzerDriftForGlobals()
    {
        const string localRoot = "<Project>\n  <PropertyGroup>\n    <OrganizationName>KL</OrganizationName>\n  </PropertyGroup>\n</Project>";

        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile("Directory.Build.props", localRoot);
        using var logger = testOutput.BuildLogger();
        var required = new List<CpmRequiredEntry>();

        var actual = DirectoryBuildPropsHelper.HandleFileWithCentralPackageManagement(
            logger,
            "root",
            TestFixtures.DistributionRootProps(),
            workspace.Root,
            Detect(workspace),
            required);

        actual.Outcome.Should().Be(FileUpdateOutcome.Unchanged);
        workspace.ReadFile("Directory.Build.props").Should().Be(localRoot);
        actual.Drift.PackageReferencesOnlyUpstream.Should().BeEmpty();
        required.Should().BeEmpty();
    }

    [Fact]
    public void HandleFileWithCpm_SkipsCreation_WhenThereIsNoPackagesPropsToDeclareVersionsIn()
    {
        const string buildPropsWithFlag = "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>";

        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Build.props", buildPropsWithFlag);
        using var logger = testOutput.BuildLogger();
        var required = new List<CpmRequiredEntry>();

        var actual = DirectoryBuildPropsHelper.HandleFileWithCentralPackageManagement(
            logger,
            "test",
            TestFixtures.DistributionTestProps(),
            workspace.Directory("test"),
            Detect(workspace),
            required);

        actual.Outcome.Should().Be(FileUpdateOutcome.Skipped);
        workspace.FileExists("test/Directory.Build.props").Should().BeFalse();
        required.Should().BeEmpty();
    }

    [Fact]
    public void HandleFileWithCpm_CreatesRootPropsWithoutAnalyzers_AndRequiresGlobalReferences()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");
        using var logger = testOutput.BuildLogger();
        var required = new List<CpmRequiredEntry>();

        DirectoryBuildPropsHelper.HandleFileWithCentralPackageManagement(
            logger,
            "root",
            TestFixtures.DistributionRootProps(),
            workspace.Root,
            Detect(workspace),
            required);

        PackageItemParser.Parse(workspace.ReadFile("Directory.Build.props"))!.Should().BeEmpty();
        required.Should().HaveCount(5).And.OnlyContain(x => x.ItemType == PackageItemType.GlobalPackageReference);
    }

    private static CentralPackageManagementContext Detect(
        TestWorkspace workspace)
        => CentralPackageManagementHelper.Detect(workspace.Root);
}
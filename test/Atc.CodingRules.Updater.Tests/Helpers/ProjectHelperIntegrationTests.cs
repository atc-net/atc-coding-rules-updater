namespace Atc.CodingRules.Updater.Tests.Helpers;

/// <summary>
/// End-to-end runs of <see cref="ProjectHelper.HandleFiles"/> against throw-away repositories.
/// They download the real atc-coding-rules distribution, ask the ATC API for package versions,
/// and restore the result with the real <c>dotnet</c> CLI, so they need network access.
/// </summary>
[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class ProjectHelperIntegrationTests
{
    private const string PackagesProps = """
        <Project>

          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
          </PropertyGroup>

        </Project>
        """;

    private const string TestProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    private readonly ITestOutputHelper testOutput;

    public ProjectHelperIntegrationTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public async Task Run_OnCpmRepository_MovesVersionsIntoPackagesProps()
    {
        using var workspace = CreateCpmRepository();
        using var logger = testOutput.BuildLogger();

        var summary = await ProjectHelper.HandleFiles(logger, workspace.Root, Options(workspace), TestContext.Current.CancellationToken);

        summary.CentralPackageManagement.Should().BeTrue();
        PackageItemParser.Parse(workspace.ReadFile("test/Directory.Build.props"))!
            .Should().NotBeEmpty().And.OnlyContain(x => x.Version == null);

        var packagesProps = PackageItemParser.Parse(workspace.ReadFile("Directory.Packages.props"))!;
        packagesProps.Should().Contain(x => x.Id == "AsyncFixer" && x.ItemType == PackageItemType.GlobalPackageReference);
        packagesProps.Should().Contain(x => x.Id == "Atc.Test" && x.ItemType == PackageItemType.PackageVersion);
    }

    // The CPM layout must restore exactly as well as the non-CPM layout of the same distribution:
    // it may not add a single restore error, and never the CPM ones (NU1008, NU1010). Comparing
    // against the non-CPM layout keeps the test meaningful even when the distribution itself
    // references a package that does not restore.
    [Fact]
    public async Task Run_OnCpmRepository_RestoresAsWellAsTheNonCpmLayout()
    {
        using var cpm = CreateCpmRepository();
        using var nonCpm = new TestWorkspace();
        nonCpm.WriteFile("test/App.Tests/App.Tests.csproj", TestProject);
        using var logger = testOutput.BuildLogger();
        var cancellationToken = TestContext.Current.CancellationToken;

        await ProjectHelper.HandleFiles(logger, nonCpm.Root, Options(nonCpm), cancellationToken);
        await ProjectHelper.HandleFiles(logger, cpm.Root, Options(cpm), cancellationToken);

        var nonCpmRestore = await DotnetCli.Run(nonCpm.Root, "restore", "test/App.Tests/App.Tests.csproj");
        var cpmRestore = await DotnetCli.Run(cpm.Root, "restore", "test/App.Tests/App.Tests.csproj");
        testOutput.WriteLine(cpmRestore.Output);

        cpmRestore.Output.Should().NotContain("NU1008").And.NotContain("NU1010");
        RestoreErrors(cpmRestore.Output).Should().BeSubsetOf(RestoreErrors(nonCpmRestore.Output));
        if (nonCpmRestore.ExitCode == 0)
        {
            cpmRestore.ExitCode.Should().Be(0);
        }
    }

    [Fact]
    public async Task Run_OnCpmRepository_ChangesNothingTheSecondTime()
    {
        using var workspace = CreateCpmRepository();
        using var logger = testOutput.BuildLogger();
        await ProjectHelper.HandleFiles(logger, workspace.Root, Options(workspace), TestContext.Current.CancellationToken);
        var packagesPropsAfterFirstRun = workspace.ReadFile("Directory.Packages.props");

        var second = await ProjectHelper.HandleFiles(logger, workspace.Root, Options(workspace), TestContext.Current.CancellationToken);

        second.HasChanges.Should().BeFalse();
        workspace.ReadFile("Directory.Packages.props").Should().Be(packagesPropsAfterFirstRun);
    }

    [Fact]
    public async Task Run_OnCpmRepository_DryRunReportsWhatTheRealRunDoes_AndWritesNothing()
    {
        using var workspace = CreateCpmRepository();
        using var logger = testOutput.BuildLogger();
        var dryRunOptions = Options(workspace);
        dryRunOptions.DryRun = true;

        var dryRun = await ProjectHelper.HandleFiles(logger, workspace.Root, dryRunOptions, TestContext.Current.CancellationToken);

        workspace.ReadFile("Directory.Packages.props").Should().Be(PackagesProps);
        workspace.FileExists("test/Directory.Build.props").Should().BeFalse();

        var real = await ProjectHelper.HandleFiles(logger, workspace.Root, Options(workspace), TestContext.Current.CancellationToken);

        dryRun.Files.Select(x => (x.Area, x.File, x.Outcome))
            .Should().Equal(real.Files.Select(x => (x.Area, x.File, x.Outcome)));
    }

    [Fact]
    public async Task Run_OnNonCpmRepository_BehavesAsBefore()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("test/App.Tests/App.Tests.csproj", TestProject);
        using var logger = testOutput.BuildLogger();
        var options = Options(workspace);
        options.UseLatestMinorNugetVersion = false;

        var summary = await ProjectHelper.HandleFiles(logger, workspace.Root, options, TestContext.Current.CancellationToken);

        summary.CentralPackageManagement.Should().BeNull();
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
        logger.Entries.Should().ContainSingle(x => x.Message.Contains("cpm migrate", StringComparison.Ordinal));
        PackageItemParser.Parse(workspace.ReadFile("test/Directory.Build.props"))!
            .Should().NotBeEmpty().And.OnlyContain(x => x.Version != null);
    }

    /// <summary>Each distinct "error NUxxxx: …" line, with the machine-specific project path removed.</summary>
    private static HashSet<string> RestoreErrors(string output)
        => Regex.Matches(output, @"error (?<code>NU\d{4}): (?<message>.*?)(?: \[|$)", RegexOptions.Multiline | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1))
            .Select(x => $"{x.Groups["code"].Value}: {x.Groups["message"].Value.Trim()}")
            .ToHashSet(StringComparer.Ordinal);

    private static TestWorkspace CreateCpmRepository()
    {
        var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile("test/App.Tests/App.Tests.csproj", TestProject);
        return workspace;
    }

    private static OptionsFile Options(TestWorkspace workspace)
    {
        var options = new OptionsFile
        {
            ProjectTarget = SupportedProjectTargetType.DotNet10,
        };

        options.Mappings.Test.Paths.Add(workspace.Directory("test").FullName);
        return options;
    }
}
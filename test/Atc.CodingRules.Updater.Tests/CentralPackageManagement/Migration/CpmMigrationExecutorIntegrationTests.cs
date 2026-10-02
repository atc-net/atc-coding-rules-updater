namespace Atc.CodingRules.Updater.Tests.CentralPackageManagement.Migration;

/// <summary>
/// <c>cpm migrate</c> against real repositories: real git, real <c>dotnet restore</c> and real
/// packages from nuget.org, so they need network access.
/// </summary>
[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class CpmMigrationExecutorIntegrationTests
{
    private readonly ITestOutputHelper testOutput;

    public CpmMigrationExecutorIntegrationTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public async Task IsWorkingTreeClean_TracksGitState()
    {
        using var workspace = new TestWorkspace();
        var environment = new DotnetCpmMigrationEnvironment();
        var cancellationToken = TestContext.Current.CancellationToken;

        (await environment.IsWorkingTreeCleanAsync(workspace.Root, cancellationToken)).Should().BeNull();

        workspace.WriteFile("README.md", "x");
        await InitializeGitRepository(workspace);
        (await environment.IsWorkingTreeCleanAsync(workspace.Root, cancellationToken)).Should().BeTrue();

        workspace.WriteFile("README.md", "changed");
        (await environment.IsWorkingTreeCleanAsync(workspace.Root, cancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Restore_CapturesResolvedPackagesPerProject()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/A/A.csproj", Project("Newtonsoft.Json", "13.0.3"));
        var environment = new DotnetCpmMigrationEnvironment();

        var actual = await environment.RestoreAsync(workspace.Root, "src/A/A.csproj", "Release", TestContext.Current.CancellationToken);

        actual.ExitCode.Should().Be(0);
        actual.Packages.Should().ContainKey("src/A/obj/project.assets.json")
            .WhoseValue.Should().Contain("net10.0|Newtonsoft.Json/13.0.3");
    }

    [Fact]
    public async Task Migrate_ConvertsARealRepository_AndPreservesWhatRestores()
    {
        using var workspace = await CreateRepository();

        var actual = await Migrate(workspace, new CpmMigrationOptions { RestoreTarget = "App.slnx" });

        testOutput.WriteLine(string.Join(Environment.NewLine, actual.Differences));
        actual.Outcome.Should().Be(CpmMigrationOutcome.Migrated);
        workspace.ReadFile("src/B/B.csproj").Should().Contain("VersionOverride=\"13.0.1\"");
        PackageItemParser.Parse(workspace.ReadFile("Directory.Packages.props"))!
            .Should().Contain(x => x.Id == "AsyncFixer" && x.ItemType == PackageItemType.GlobalPackageReference);

        var (exitCode, output) = await DotnetCli.Run(workspace.Root, "restore", "App.slnx");
        testOutput.WriteLine(output);
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task Migrate_RollsBack_WhenAProjectWouldGainGlobalAnalyzers()
    {
        using var workspace = await CreateRepository(withIsolatedProject: true);
        var before = SnapshotFiles(workspace);

        var actual = await Migrate(workspace, new CpmMigrationOptions { RestoreTarget = "App.slnx" });

        testOutput.WriteLine(string.Join(Environment.NewLine, actual.Differences));
        actual.Outcome.Should().Be(CpmMigrationOutcome.RolledBack);
        actual.Differences.Should().Contain(x => x.Contains("Isolated", StringComparison.Ordinal) && x.Contains("gained", StringComparison.Ordinal));
        SnapshotFiles(workspace).Should().BeEquivalentTo(before);
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
    }

    [Fact]
    public async Task Migrate_Blocks_WhenAProjectIsOutsideTheRestoredSolution()
    {
        using var workspace = await CreateRepository();
        workspace.WriteFile("tools/Script/Script.csproj", Project("Newtonsoft.Json", "13.0.3"));
        await Git(workspace, "add", "-A");
        await Git(workspace, "-c", "user.name=test", "-c", "user.email=test@example.com", "commit", "-q", "-m", "script");

        var actual = await Migrate(workspace, new CpmMigrationOptions { RestoreTarget = "App.slnx" });

        actual.Outcome.Should().Be(CpmMigrationOutcome.Blocked);
        actual.Plan.Blockers.Should().ContainSingle(x => x.Code == "ProjectNotCoveredByRestore");
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
    }

    private static string Project(
        string packageId,
        string version)
        => $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{packageId}" Version="{version}" />
              </ItemGroup>
            </Project>
            """;

    private static async Task<TestWorkspace> CreateRepository(
        bool withIsolatedProject = false)
    {
        var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Build.props", RootPropsWithAnalyzers());
        workspace.WriteFile("src/A/A.csproj", Project("Newtonsoft.Json", "13.0.3"));
        workspace.WriteFile("src/B/B.csproj", Project("Newtonsoft.Json", "13.0.1"));

        var projects = new List<string> { "src/A/A.csproj", "src/B/B.csproj" };
        if (withIsolatedProject)
        {
            // Its own Directory.Build.props stops the root one from being imported, so it has no
            // analyzers today - but a GlobalPackageReference would still reach it.
            workspace.WriteFile("tools/Isolated/Directory.Build.props", "<Project />");
            workspace.WriteFile("tools/Isolated/Isolated.csproj", Project("Newtonsoft.Json", "13.0.3"));
            projects.Add("tools/Isolated/Isolated.csproj");
        }

        workspace.WriteFile(
            "App.slnx",
            "<Solution>\n" + string.Concat(projects.Select(x => $"  <Project Path=\"{x}\" />\n")) + "</Solution>\n");

        await InitializeGitRepository(workspace);
        return workspace;
    }

    /// <summary>A root props with a small analyzer group, and no TreatWarningsAsErrors so the restores stay about packages.</summary>
    private static string RootPropsWithAnalyzers()
        => """
           <Project>
             <ItemGroup Label="Code Analyzers">
               <PackageReference Include="AsyncFixer" Version="2.1.0" PrivateAssets="All" />
             </ItemGroup>
           </Project>
           """;

    private static async Task InitializeGitRepository(TestWorkspace workspace)
    {
        await Git(workspace, "init", "-q");
        await Git(workspace, "add", "-A");
        await Git(workspace, "-c", "user.name=test", "-c", "user.email=test@example.com", "commit", "-q", "-m", "initial");
    }

    private static async Task Git(
        TestWorkspace workspace,
        params string[] arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = workspace.Root.FullName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(startInfo)!;
        await process.WaitForExitAsync();
        process.ExitCode.Should().Be(0);
    }

    private static Dictionary<string, string> SnapshotFiles(
        TestWorkspace workspace)
        => Directory
            .EnumerateFiles(workspace.Root.FullName, "*", SearchOption.AllDirectories)
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                        !x.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToDictionary(x => x, File.ReadAllText, StringComparer.Ordinal);

    private Task<CpmMigrationResult> Migrate(
        TestWorkspace workspace,
        CpmMigrationOptions options)
    {
        var logger = testOutput.BuildLogger();
        return CpmMigrationExecutor.ExecuteAsync(
            logger,
            workspace.Root,
            options,
            new DotnetCpmMigrationEnvironment(),
            TestContext.Current.CancellationToken);
    }
}
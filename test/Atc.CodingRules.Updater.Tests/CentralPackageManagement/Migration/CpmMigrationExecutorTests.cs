namespace Atc.CodingRules.Updater.Tests.CentralPackageManagement.Migration;

/// <summary>
/// The decision logic of <see cref="CpmMigrationExecutor"/>. Git and <c>dotnet restore</c> are
/// replaced by <see cref="FakeMigrationEnvironment"/>, so every path, including rollback, can be
/// driven deterministically.
/// </summary>
public sealed class CpmMigrationExecutorTests
{
    private const string ProjectContent = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Atc\" Version=\"3.0.186\" />\n  </ItemGroup>\n</Project>";

    private static readonly RestoreSnapshot CleanRestore = new(
        0,
        new HashSet<string>(StringComparer.Ordinal),
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["src/App/obj/project.assets.json"] = new HashSet<string>(StringComparer.Ordinal) { "net10.0|Atc/3.0.186" },
        });

    private readonly ITestOutputHelper testOutput;

    public CpmMigrationExecutorTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public async Task Execute_ReportsAlreadyMigrated_AndWritesNothing()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");
        var environment = new FakeMigrationEnvironment();

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.AlreadyMigrated);
        environment.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Execute_ReportsBlocked_AndWritesNothing_WhenPlanHasBlockers()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent.Replace("3.0.186", "3.*", StringComparison.Ordinal));
        var environment = new FakeMigrationEnvironment();

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.Blocked);
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
    }

    [Fact]
    public async Task Execute_Blocks_WhenWorkingTreeIsDirty()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment { IsClean = false };

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.Blocked);
        actual.Plan.Blockers.Should().ContainSingle(x => x.Code == "DirtyWorkingTree");
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
    }

    [Fact]
    public async Task Execute_Blocks_WhenNotAGitRepository()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment { IsClean = null };

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Plan.Blockers.Should().ContainSingle(x => x.Code == "NotAGitRepository");
    }

    [Fact]
    public async Task Execute_Migrates_WhenDirtyTreeIsAllowed()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment { IsClean = false };
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore);

        var actual = await Execute(workspace, environment, new CpmMigrationOptions { AllowDirty = true });

        actual.Outcome.Should().Be(CpmMigrationOutcome.Migrated);
    }

    [Fact]
    public async Task Execute_DryRun_WritesNothing_AndDoesNotRestore()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment { IsClean = false };

        var actual = await Execute(workspace, environment, new CpmMigrationOptions { DryRun = true });

        actual.Outcome.Should().Be(CpmMigrationOutcome.DryRun);
        actual.Plan.FileChanges.Should().NotBeEmpty();
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
        workspace.ReadFile("src/App/App.csproj").Should().Be(ProjectContent);
        environment.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Execute_Blocks_WhenBaselineRestoreFails()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore with { ExitCode = 1 });

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.Blocked);
        actual.Plan.Blockers.Should().ContainSingle(x => x.Code == "BaselineRestoreFailed");
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
    }

    [Fact]
    public async Task Execute_Blocks_WhenAProjectIsNotCoveredByTheRestore()
    {
        // Directory.Packages.props and GlobalPackageReference reach every project under the
        // root, so a project the restore never saw could change without being verified.
        using var workspace = CreateRepository();
        workspace.WriteFile("tools/Script/Script.csproj", ProjectContent);
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.Blocked);
        actual.Plan.Blockers.Should().ContainSingle(x => x.Code == "ProjectNotCoveredByRestore")
            .Which.Message.Should().Contain("Script.csproj");
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
        environment.RestoreCalls.Should().Be(1);
    }

    [Fact]
    public async Task Execute_WritesPlannedFiles_WhenRestoreIsUnchanged()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore);

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.Migrated);
        environment.RestoreCalls.Should().Be(2);
        workspace.ReadFile("src/App/App.csproj").Should().Contain("<PackageReference Include=\"Atc\" />");
        workspace.FileExists("Directory.Packages.props").Should().BeTrue();
    }

    [Fact]
    public async Task Execute_RollsBack_WhenAProjectGainsAPackage()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore with
        {
            Packages = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["src/App/obj/project.assets.json"] = new HashSet<string>(StringComparer.Ordinal) { "net10.0|Atc/3.0.186", "net10.0|AsyncFixer/2.1.0" },
            },
        });

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.RolledBack);
        actual.Differences.Should().ContainSingle().Which.Should().Contain("AsyncFixer/2.1.0");
        AssertRolledBack(workspace);
    }

    [Fact]
    public async Task Execute_RollsBack_WhenAVersionChanges()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore with
        {
            Packages = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["src/App/obj/project.assets.json"] = new HashSet<string>(StringComparer.Ordinal) { "net10.0|Atc/3.0.200" },
            },
        });

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.RolledBack);
        AssertRolledBack(workspace);
    }

    [Fact]
    public async Task Execute_RollsBack_WhenRestoreRaisesANewWarning()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore with
        {
            Warnings = new HashSet<string>(StringComparer.Ordinal) { "NU1507: There are 2 package sources defined in your configuration." },
        });

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.RolledBack);
        actual.Differences.Should().ContainSingle().Which.Should().Contain("NU1507");
        AssertRolledBack(workspace);
    }

    [Fact]
    public async Task Execute_RollsBack_WhenRestoreFailsAfterWriting()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore with { ExitCode = 1 });

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.RolledBack);
        AssertRolledBack(workspace);
    }

    [Fact]
    public async Task Execute_RollsBackByteForByte_IncludingAByteOrderMark()
    {
        using var workspace = new TestWorkspace();
        var project = Path.Combine(workspace.Root.FullName, "src", "App", "App.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(project)!);
        var original = WithBom(ProjectContent);
        await File.WriteAllBytesAsync(project, original, TestContext.Current.CancellationToken);
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore with { ExitCode = 1 });

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.RolledBack);
        (await File.ReadAllBytesAsync(project, TestContext.Current.CancellationToken)).Should().Equal(original);
    }

    [Fact]
    public async Task Execute_KeepsAByteOrderMark_WhenMigrating()
    {
        using var workspace = new TestWorkspace();
        var project = Path.Combine(workspace.Root.FullName, "src", "App", "App.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(project)!);
        await File.WriteAllBytesAsync(project, WithBom(ProjectContent), TestContext.Current.CancellationToken);
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore);

        await Execute(workspace, environment, new CpmMigrationOptions());

        (await File.ReadAllBytesAsync(project, TestContext.Current.CancellationToken)).Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
    }

    [Fact]
    public async Task Execute_ReportsIt_WhenTheWorkingTreeIsNotCleanAfterRollback()
    {
        // Restore can rewrite tracked files the migration never backed up (packages.lock.json).
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.CleanStates.Enqueue(true);
        environment.CleanStates.Enqueue(false);
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore with { ExitCode = 1 });

        var actual = await Execute(workspace, environment, new CpmMigrationOptions());

        actual.Outcome.Should().Be(CpmMigrationOutcome.RolledBack);
        actual.Differences.Should().Contain(x => x.Contains("git status", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Execute_SkipsRestore_WhenVerificationIsSkipped()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();

        var actual = await Execute(workspace, environment, new CpmMigrationOptions { SkipRestoreVerification = true });

        actual.Outcome.Should().Be(CpmMigrationOutcome.Migrated);
        environment.RestoreCalls.Should().Be(0);
    }

    [Fact]
    public async Task Execute_IgnoresPackageChanges_ButNotWarnings_WhenUnifyingVersions()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore with
        {
            Packages = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["src/App/obj/project.assets.json"] = new HashSet<string>(StringComparer.Ordinal) { "net10.0|Atc/3.0.200" },
            },
        });

        var actual = await Execute(workspace, environment, new CpmMigrationOptions { UnifyVersions = true });

        actual.Outcome.Should().Be(CpmMigrationOutcome.Migrated);
    }

    [Fact]
    public async Task Execute_PassesBuildConfigurationAndTarget_ToRestore()
    {
        using var workspace = CreateRepository();
        var environment = new FakeMigrationEnvironment();
        environment.Snapshots.Enqueue(CleanRestore);
        environment.Snapshots.Enqueue(CleanRestore);

        await Execute(workspace, environment, new CpmMigrationOptions { RestoreTarget = "My.slnx", BuildConfiguration = "Debug" });

        environment.LastRestoreTarget.Should().Be("My.slnx");
        environment.LastConfiguration.Should().Be("Debug");
    }

    private static TestWorkspace CreateRepository()
    {
        var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent);
        return workspace;
    }

    private static byte[] WithBom(string content)
        => [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(content)];

    private static void AssertRolledBack(TestWorkspace workspace)
    {
        workspace.ReadFile("src/App/App.csproj").Should().Be(ProjectContent);
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
    }

    private Task<CpmMigrationResult> Execute(
        TestWorkspace workspace,
        FakeMigrationEnvironment environment,
        CpmMigrationOptions options)
    {
        var logger = testOutput.BuildLogger();
        return CpmMigrationExecutor.ExecuteAsync(logger, workspace.Root, options, environment, TestContext.Current.CancellationToken);
    }

    private sealed class FakeMigrationEnvironment : ICpmMigrationEnvironment
    {
        public bool? IsClean { get; init; } = true;

        /// <summary>When not empty, answers the git checks in order instead of <see cref="IsClean"/>.</summary>
        public Queue<bool?> CleanStates { get; } = new();

        public Queue<RestoreSnapshot> Snapshots { get; } = new();

        public int RestoreCalls { get; private set; }

        public string? LastRestoreTarget { get; private set; }

        public string? LastConfiguration { get; private set; }

        public Task<bool?> IsWorkingTreeCleanAsync(
            DirectoryInfo projectPath,
            CancellationToken cancellationToken)
            => Task.FromResult(CleanStates.Count > 0 ? CleanStates.Dequeue() : IsClean);

        public Task<RestoreSnapshot> RestoreAsync(
            DirectoryInfo projectPath,
            string? restoreTarget,
            string configuration,
            CancellationToken cancellationToken)
        {
            RestoreCalls++;
            LastRestoreTarget = restoreTarget;
            LastConfiguration = configuration;
            return Task.FromResult(Snapshots.Dequeue());
        }
    }
}
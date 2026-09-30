namespace Atc.CodingRules.Updater.Tests.Commands;

public sealed class MigrateToCpmCommandTests
{
    private const string ProjectContent = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Atc\" Version=\"3.0.186\" />\n  </ItemGroup>\n</Project>";

    [Theory]
    [InlineData(CpmMigrationOutcome.Migrated, 0)]
    [InlineData(CpmMigrationOutcome.AlreadyMigrated, 0)]
    [InlineData(CpmMigrationOutcome.NothingToMigrate, 0)]
    [InlineData(CpmMigrationOutcome.DryRun, 0)]
    [InlineData(CpmMigrationOutcome.Blocked, 1)]
    [InlineData(CpmMigrationOutcome.RolledBack, 1)]
    public void MapExitCode_FailsOnlyWhenBlockedOrRolledBack(
        CpmMigrationOutcome outcome,
        int expected)
    {
        var actual = MigrateToCpmCommand.MapExitCode(outcome);

        actual.Should().Be(expected == 0 ? ConsoleExitStatusCodes.Success : ConsoleExitStatusCodes.Failure);
    }

    [Fact]
    public async Task ExecuteInternalAsync_DryRun_ReturnsSuccess_AndWritesNothing()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent);
        var command = new MigrateToCpmCommand(NullLogger<MigrateToCpmCommand>.Instance);

        var actual = await command.ExecuteInternalAsync(
            new MigrateToCpmCommandSettings { ProjectPath = workspace.Root.FullName, DryRun = true },
            new ThrowingEnvironment(),
            TestContext.Current.CancellationToken);

        actual.Should().Be(ConsoleExitStatusCodes.Success);
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
        workspace.ReadFile("src/App/App.csproj").Should().Be(ProjectContent);
    }

    [Fact]
    public async Task ExecuteInternalAsync_ReturnsFailure_WhenBlocked()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent.Replace("3.0.186", "3.*", StringComparison.Ordinal));
        var command = new MigrateToCpmCommand(NullLogger<MigrateToCpmCommand>.Instance);

        var actual = await command.ExecuteInternalAsync(
            new MigrateToCpmCommandSettings { ProjectPath = workspace.Root.FullName },
            new ThrowingEnvironment(),
            TestContext.Current.CancellationToken);

        actual.Should().Be(ConsoleExitStatusCodes.Failure);
    }

    [Fact]
    public async Task CreateOptions_ResolvesBuildFileAgainstProjectPath_AndDefaultsToRelease()
    {
        using var workspace = new TestWorkspace();
        var settings = new MigrateToCpmCommandSettings
        {
            ProjectPath = workspace.Root.FullName,
            BuildFile = new FlagValue<string> { IsSet = true, Value = "App.slnx" },
        };

        var actual = await MigrateToCpmCommand.CreateOptions(settings, workspace.Root, TestContext.Current.CancellationToken);

        actual.RestoreTarget.Should().Be(Path.Combine(workspace.Root.FullName, "App.slnx"));
        actual.BuildConfiguration.Should().Be("Release");
    }

    [Fact]
    public async Task CreateOptions_UsesBuildSettingsFromOptionsFile()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile(
            "atc-coding-rules-updater.json",
            """{ "projectTarget": "DotNet10", "buildFile": "My.slnx", "buildConfiguration": "Debug" }""");

        var actual = await MigrateToCpmCommand.CreateOptions(
            new MigrateToCpmCommandSettings { ProjectPath = workspace.Root.FullName },
            workspace.Root,
            TestContext.Current.CancellationToken);

        actual.RestoreTarget.Should().Be(Path.Combine(workspace.Root.FullName, "My.slnx"));
        actual.BuildConfiguration.Should().Be("Debug");
    }

    [Fact]
    public void SerializeJson_ContainsOutcomeBlockersAndChangedFiles()
    {
        var plan = new CpmMigrationPlan
        {
            Blockers = [new CpmMigrationBlocker("FloatingVersion", "A uses 1.*", "/repo/A.csproj")],
        };

        var actual = MigrateToCpmCommand.SerializeJson(new CpmMigrationResult(CpmMigrationOutcome.Blocked, plan, []));

        actual.Should().Contain("\"Outcome\": \"Blocked\"").And.Contain("FloatingVersion").And.Contain("\"ChangedFiles\"");
    }

    private sealed class ThrowingEnvironment : ICpmMigrationEnvironment
    {
        public Task<bool?> IsWorkingTreeCleanAsync(
            DirectoryInfo projectPath,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Not expected in this test.");

        public Task<RestoreSnapshot> RestoreAsync(
            DirectoryInfo projectPath,
            string? restoreTarget,
            string configuration,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Not expected in this test.");
    }
}
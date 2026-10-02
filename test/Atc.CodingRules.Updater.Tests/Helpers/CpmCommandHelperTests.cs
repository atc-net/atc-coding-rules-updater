namespace Atc.CodingRules.Updater.Tests.Helpers;

public sealed class CpmCommandHelperTests
{
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
        var actual = CpmCommandHelper.MapExitCode(outcome);

        actual.Should().Be(expected == 0 ? ConsoleExitStatusCodes.Success : ConsoleExitStatusCodes.Failure);
    }

    [Fact]
    public async Task CreateOptions_ResolvesBuildFileAgainstProjectPath_AndDefaultsToRelease()
    {
        using var workspace = new TestWorkspace();
        var settings = new CpmMigrateCommandSettings
        {
            ProjectPath = workspace.Root.FullName,
            BuildFile = new FlagValue<string> { IsSet = true, Value = "App.slnx" },
        };

        var actual = await CpmCommandHelper.CreateOptions(settings, workspace.Root, TestContext.Current.CancellationToken);

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

        var actual = await CpmCommandHelper.CreateOptions(
            new CpmMigrateCommandSettings { ProjectPath = workspace.Root.FullName },
            workspace.Root,
            TestContext.Current.CancellationToken);

        actual.RestoreTarget.Should().Be(Path.Combine(workspace.Root.FullName, "My.slnx"));
        actual.BuildConfiguration.Should().Be("Debug");
    }

    [Fact]
    public async Task CreateOptions_TakesTheMigrateFlags()
    {
        using var workspace = new TestWorkspace();
        var settings = new CpmMigrateCommandSettings
        {
            ProjectPath = workspace.Root.FullName,
            DryRun = true,
            UnifyVersions = true,
            SkipRestoreVerification = true,
            AllowDirty = true,
        };

        var actual = await CpmCommandHelper.CreateOptions(settings, workspace.Root, TestContext.Current.CancellationToken);

        actual.Should().Match<CpmMigrationOptions>(x =>
            x.DryRun && x.UnifyVersions && x.SkipRestoreVerification && x.AllowDirty);
    }

    [Fact]
    public async Task CreateOptions_AlwaysDryRuns_ForValidateSettings()
    {
        using var workspace = new TestWorkspace();
        var settings = new CpmCommandSettings
        {
            ProjectPath = workspace.Root.FullName,
            UnifyVersions = true,
        };

        var actual = await CpmCommandHelper.CreateOptions(settings, workspace.Root, TestContext.Current.CancellationToken);

        actual.DryRun.Should().BeTrue();
        actual.UnifyVersions.Should().BeTrue();
    }

    [Fact]
    public void SerializeJson_ContainsOutcomeBlockersAndChangedFiles()
    {
        var plan = new CpmMigrationPlan
        {
            Blockers = [new CpmMigrationBlocker("FloatingVersion", "A uses 1.*", "/repo/A.csproj")],
        };

        var actual = CpmCommandHelper.SerializeJson(new CpmMigrationResult(CpmMigrationOutcome.Blocked, plan, []));

        actual.Should().Contain("\"Outcome\": \"Blocked\"").And.Contain("FloatingVersion").And.Contain("\"ChangedFiles\"");
    }
}
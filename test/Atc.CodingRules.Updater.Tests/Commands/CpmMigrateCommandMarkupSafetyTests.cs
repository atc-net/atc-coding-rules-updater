namespace Atc.CodingRules.Updater.Tests.Commands;

/// <summary>
/// Log messages are rendered as Spectre markup by the console sink, so every message
/// <c>cpm migrate</c> logs must survive being parsed as markup. A blocker code in square
/// brackets did not, and crashed the command with "Could not find color or style".
/// </summary>
public sealed class CpmMigrateCommandMarkupSafetyTests
{
    private const string ProjectContent = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Atc\" Version=\"3.0.186\" />\n  </ItemGroup>\n</Project>";

    private readonly ITestOutputHelper testOutput;

    public CpmMigrateCommandMarkupSafetyTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public async Task LogsMarkupSafeMessages_WhenPlanIsBlocked()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/[Weird]/App.csproj", ProjectContent.Replace("3.0.186", "[1.0,2.0)*", StringComparison.Ordinal));

        await AssertMarkupSafe(workspace, new FakeEnvironment());
    }

    [Fact]
    public async Task LogsMarkupSafeMessages_WhenNotAGitRepository()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent);

        await AssertMarkupSafe(workspace, new FakeEnvironment { IsClean = null });
    }

    [Fact]
    public async Task LogsMarkupSafeMessages_WhenRolledBack()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent);
        var environment = new FakeEnvironment();
        environment.Snapshots.Enqueue(Snapshot(0, "net10.0|Atc/3.0.186"));
        environment.Snapshots.Enqueue(Snapshot(0, "net10.0|Atc/3.0.186", "net10.0|Odd.[Name]/1.0.0"));

        await AssertMarkupSafe(workspace, environment);
    }

    [Fact]
    public async Task LogsMarkupSafeMessages_WhenMigratedWithOverrides()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/[A]/A.csproj", ProjectContent);
        workspace.WriteFile("src/[B]/B.csproj", ProjectContent.Replace("3.0.186", "3.0.100", StringComparison.Ordinal));
        var environment = new FakeEnvironment();
        var covered = new RestoreSnapshot(
            0,
            new HashSet<string>(StringComparer.Ordinal),
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["src/[A]/obj/project.assets.json"] = new HashSet<string>(StringComparer.Ordinal),
                ["src/[B]/obj/project.assets.json"] = new HashSet<string>(StringComparer.Ordinal),
            });
        environment.Snapshots.Enqueue(covered);
        environment.Snapshots.Enqueue(covered);

        var outcome = await AssertMarkupSafe(workspace, environment);

        outcome.Should().Be(ConsoleExitStatusCodes.Success, "the migrated path, with its VersionOverride lines, must be the one exercised");
    }

    private static RestoreSnapshot Snapshot(
        int exitCode,
        params string[] packages)
        => new(
            exitCode,
            new HashSet<string>(StringComparer.Ordinal),
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["src/App/obj/project.assets.json"] = packages.ToHashSet(StringComparer.Ordinal),
            });

    private async Task<int> AssertMarkupSafe(
        TestWorkspace workspace,
        FakeEnvironment environment)
    {
        using var logger = testOutput.BuildLoggerFor<CpmMigrateCommand>(LogLevel.Trace);
        var command = new CpmMigrateCommand(logger);

        var exitCode = await command.ExecuteInternalAsync(
            new CpmMigrateCommandSettings { ProjectPath = workspace.Root.FullName },
            environment,
            TestContext.Current.CancellationToken);

        logger.Entries.Should().NotBeEmpty();
        foreach (var message in logger.Entries.Select(x => x.Message))
        {
            // Constructing a Markup is what the console sink does; it throws on a malformed tag.
            var exception = Record.Exception(() => new Spectre.Console.Markup(message));
            exception.Should().BeNull($"the logged message must be renderable as markup, but was: {message}");
        }

        return exitCode;
    }

    private sealed class FakeEnvironment : ICpmMigrationEnvironment
    {
        public bool? IsClean { get; init; } = true;

        public Queue<RestoreSnapshot> Snapshots { get; } = new();

        public Task<bool?> IsWorkingTreeCleanAsync(
            DirectoryInfo projectPath,
            CancellationToken cancellationToken)
            => Task.FromResult(IsClean);

        public Task<RestoreSnapshot> RestoreAsync(
            DirectoryInfo projectPath,
            string? restoreTarget,
            string configuration,
            CancellationToken cancellationToken)
            => Task.FromResult(Snapshots.Dequeue());
    }
}
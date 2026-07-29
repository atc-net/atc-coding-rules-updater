namespace Atc.CodingRules.Updater.Tests.Commands;

/// <summary>
/// Characterization tests for <see cref="RunCommand.GetBuildFile"/>. These pin the existing
/// behaviour so the colon-based "is this absolute?" heuristic can be replaced by a plain
/// <see cref="Path.Combine(string, string)"/> without changing what callers observe.
/// </summary>
public sealed class RunCommandTests
{
    private static readonly DirectoryInfo ProjectPath = new(
        Path.Combine(Path.GetTempPath(), "atc-coding-rules-updater-run-command-test"));

    [Fact]
    public void GetBuildFile_ReturnsNull_WhenBuildFileNotSet()
    {
        var actual = RunCommand.GetBuildFile(CreateSettings(buildFile: null), ProjectPath);

        actual.Should().BeNull();
    }

    [Fact]
    public void GetBuildFile_ReturnsNull_WhenBuildFileIsEmpty()
    {
        var actual = RunCommand.GetBuildFile(CreateSettings(string.Empty), ProjectPath);

        actual.Should().BeNull();
    }

    [Fact]
    public void GetBuildFile_ResolvesRelativeFileNameAgainstProjectPath()
    {
        var actual = RunCommand.GetBuildFile(CreateSettings("My.sln"), ProjectPath);

        actual.Should().NotBeNull();
        actual.FullName.Should().Be(Path.Combine(ProjectPath.FullName, "My.sln"));
    }

    [Fact]
    public void GetBuildFile_ResolvesRelativeSubPathAgainstProjectPath()
    {
        var actual = RunCommand.GetBuildFile(CreateSettings(Path.Combine("src", "My.csproj")), ProjectPath);

        actual.Should().NotBeNull();
        actual.FullName.Should().Be(Path.Combine(ProjectPath.FullName, "src", "My.csproj"));
    }

    [Fact]
    public void GetBuildFile_KeepsAbsolutePathUnchanged()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "elsewhere", "Other.sln");

        var actual = RunCommand.GetBuildFile(CreateSettings(absolute), ProjectPath);

        actual.Should().NotBeNull();
        actual.FullName.Should().Be(absolute);
    }

    [Fact]
    public async Task ExecuteInternalAsync_ReturnsFailure_WhenProjectPathDoesNotExist()
    {
        var command = new RunCommand(NullLogger<RunCommand>.Instance);
        var settings = new RunCommandSettings
        {
            ProjectPath = Path.Combine(Path.GetTempPath(), "atc-coding-rules-updater-does-not-exist"),
        };

        var actual = await command.ExecuteInternalAsync(settings, CancellationToken.None);

        actual.Should().Be(ConsoleExitStatusCodes.Failure);
    }

    private static RunCommandSettings CreateSettings(string? buildFile)
        => new()
        {
            ProjectPath = ProjectPath.FullName,
            BuildFile = buildFile is null
                ? new FlagValue<string>()
                : new FlagValue<string> { IsSet = true, Value = buildFile },
        };
}
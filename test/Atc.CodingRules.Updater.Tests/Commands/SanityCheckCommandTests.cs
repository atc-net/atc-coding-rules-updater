namespace Atc.CodingRules.Updater.Tests.Commands;

public sealed class SanityCheckCommandTests
{
    private static readonly string WorkingDirectory = Path.Combine(
        Path.GetTempPath(),
        "atc-coding-rules-updater-sanity-check-command-test");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteInternalAsync_ReturnsFailure_WhenErrorDiagnosticsExist(
        bool outputJson)
    {
        var directory = PrepareProjectWithEnableNetAnalyzersError(
            $"{nameof(ExecuteInternalAsync_ReturnsFailure_WhenErrorDiagnosticsExist)}-{outputJson}");

        var actual = await ExecuteAsync(directory, SupportedProjectTargetType.DotNet5, outputJson);

        actual.Should().Be(ConsoleExitStatusCodes.Failure);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteInternalAsync_ReturnsSuccess_OnCleanProject(
        bool outputJson)
    {
        var directory = PrepareCleanProject(
            $"{nameof(ExecuteInternalAsync_ReturnsSuccess_OnCleanProject)}-{outputJson}");

        var actual = await ExecuteAsync(directory, SupportedProjectTargetType.DotNet10, outputJson);

        actual.Should().Be(ConsoleExitStatusCodes.Success);
    }

    [Fact]
    public async Task ExecuteInternalAsync_ReturnsFailure_WhenProjectPathDoesNotExist()
    {
        var missing = new DirectoryInfo(
            Path.Combine(WorkingDirectory, "does-not-exist-" + nameof(ExecuteInternalAsync_ReturnsFailure_WhenProjectPathDoesNotExist)));

        var actual = await ExecuteAsync(missing, SupportedProjectTargetType.DotNet10, outputJson: false);

        actual.Should().Be(ConsoleExitStatusCodes.Failure);
    }

    private static Task<int> ExecuteAsync(
        DirectoryInfo projectPath,
        SupportedProjectTargetType projectTarget,
        bool outputJson)
    {
        var command = new SanityCheckCommand(NullLogger<SanityCheckCommand>.Instance);

        var settings = new SanityCheckCommandSettings
        {
            ProjectPath = projectPath.FullName,
            OutputJson = outputJson,
            ProjectTarget = new FlagValue<SupportedProjectTargetType>
            {
                IsSet = true,
                Value = projectTarget,
            },
        };

        return command.ExecuteInternalAsync(settings, CancellationToken.None);
    }

    private static DirectoryInfo PrepareCleanProject(string testName)
    {
        var directory = PrepareSubDirectory(testName);
        WritePropsFile(directory);
        return directory;
    }

    private static DirectoryInfo PrepareProjectWithEnableNetAnalyzersError(
        string testName)
    {
        var directory = PrepareCleanProject(testName);

        var srcDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        File.WriteAllText(
            Path.Combine(srcDirectory.FullName, "Sample.csproj"),
            string.Join(
                Environment.NewLine,
                "<Project Sdk=\"Microsoft.NET.Sdk\">",
                "  <PropertyGroup>",
                "    <TargetFramework>net5.0</TargetFramework>",
                "    <EnableNETAnalyzers>true</EnableNETAnalyzers>",
                "  </PropertyGroup>",
                "</Project>"));

        return directory;
    }

    private static DirectoryInfo PrepareSubDirectory(string testName)
    {
        var path = Path.Combine(WorkingDirectory, testName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        return Directory.CreateDirectory(path);
    }

    private static void WritePropsFile(DirectoryInfo directory)
        => File.WriteAllText(
            Path.Combine(directory.FullName, DirectoryBuildPropsHelper.FileName),
            string.Join(
                Environment.NewLine,
                "<Project>",
                "  <PropertyGroup>",
                "    <OrganizationName>Acme</OrganizationName>",
                "    <RepositoryName>my-repo</RepositoryName>",
                "  </PropertyGroup>",
                "</Project>"));
}
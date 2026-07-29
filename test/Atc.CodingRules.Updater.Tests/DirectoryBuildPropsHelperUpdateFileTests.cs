namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Covers the write path of <see cref="DirectoryBuildPropsHelper.UpdateFile"/>.
/// With <c>useLatestMinorNugetVersion: false</c> no package lookup happens, so these tests do
/// not touch the network.
/// </summary>
public sealed class DirectoryBuildPropsHelperUpdateFileTests
{
    private const string PropsContent = """
        <Project>
          <ItemGroup>
            <PackageReference Include="Meziantou.Analyzer" Version="3.0.134" />
          </ItemGroup>
        </Project>
        """;

    private static readonly string WorkingDirectory = Path.Combine(
        Path.GetTempPath(),
        "atc-coding-rules-updater-props-update-test");

    private static readonly DateTime KnownWriteTime = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly ITestOutputHelper testOutput;

    public DirectoryBuildPropsHelperUpdateFileTests(
        ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public void UpdateFile_DoesNotRewriteFile_WhenContentIsUnchanged()
    {
        var file = PrepareFile(nameof(UpdateFile_DoesNotRewriteFile_WhenContentIsUnchanged));
        using var logger = testOutput.BuildLogger();

        DirectoryBuildPropsHelper.UpdateFile(
            logger,
            file,
            PropsContent,
            "log-description-part",
            useLatestMinorNugetVersion: false);

        File.GetLastWriteTimeUtc(file.FullName).Should().Be(KnownWriteTime);
    }

    [Fact]
    public void UpdateFile_ReportsNothingToUpdate_WhenContentIsUnchanged()
    {
        var file = PrepareFile(nameof(UpdateFile_ReportsNothingToUpdate_WhenContentIsUnchanged));
        using var logger = testOutput.BuildLogger();

        DirectoryBuildPropsHelper.UpdateFile(
            logger,
            file,
            PropsContent,
            "log-description-part",
            useLatestMinorNugetVersion: false);

        logger.Entries
            .Should().Contain(x => x.Message.Contains("nothing to update", StringComparison.Ordinal));
    }

    [Fact]
    public void WouldChange_ReturnsFalse_WhenPackageBumpsAreDisabled()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        var actual = DirectoryBuildPropsHelper.WouldChange(
            logger,
            PropsContent,
            useLatestMinorNugetVersion: false);

        actual.Should().BeFalse();
    }

    private static FileInfo PrepareFile(string testName)
    {
        var path = Path.Combine(WorkingDirectory, testName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);

        var file = new FileInfo(Path.Combine(path, DirectoryBuildPropsHelper.FileName));
        File.WriteAllText(file.FullName, PropsContent);
        File.SetLastWriteTimeUtc(file.FullName, KnownWriteTime);

        return file;
    }
}
namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// The run reported outcomes only as log lines, so nothing could answer "what changed?" without
/// scraping stdout. These tests pin the outcome each path returns.
/// </summary>
public sealed class EditorConfigHelperOutcomeTests
{
    private const string UpstreamContent = """
        root = true

        [*]
        indent_size = 4
        """;

    private static readonly string WorkingDirectory = Path.Combine(
        Path.GetTempPath(),
        "atc-coding-rules-updater-editorconfig-outcome-test");

    private readonly ITestOutputHelper testOutput;

    public EditorConfigHelperOutcomeTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public void HandleFile_ReturnsSkipped_WhenUpstreamContentIsEmpty()
    {
        var actual = Execute(contentGit: string.Empty, contentFile: UpstreamContent, dryRun: false);

        actual.Should().Be(FileUpdateOutcome.Skipped);
    }

    [Fact]
    public void HandleFile_ReturnsUnchanged_WhenContentAlreadyMatches()
    {
        var actual = Execute(contentGit: UpstreamContent, contentFile: UpstreamContent, dryRun: false);

        actual.Should().Be(FileUpdateOutcome.Unchanged);
    }

    [Fact]
    public void HandleFile_ReturnsCreated_WhenLocalFileIsMissing()
    {
        var actual = Execute(contentGit: UpstreamContent, contentFile: string.Empty, dryRun: false);

        actual.Should().Be(FileUpdateOutcome.Created);
    }

    [Fact]
    public void HandleFile_ReturnsCreated_ForDryRunOnMissingLocalFile()
    {
        var actual = Execute(contentGit: UpstreamContent, contentFile: string.Empty, dryRun: true);

        actual.Should().Be(FileUpdateOutcome.Created);
    }

    [Fact]
    public void HandleFile_ReturnsUpdated_WhenTheBaseSectionDiffers()
    {
        const string localContent = """
            root = true

            [*]
            indent_size = 2


            ##########################################
            # Custom - Code Analyzers Rules
            ##########################################
            [*.{cs,csx,cake}]
            """;

        var actual = Execute(contentGit: UpstreamContent, contentFile: localContent, dryRun: false);

        actual.Should().Be(FileUpdateOutcome.Updated);
    }

    private FileUpdateOutcome Execute(
        string contentGit,
        string contentFile,
        bool dryRun)
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        var directory = Path.Combine(WorkingDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var file = new FileInfo(Path.Combine(directory, EditorConfigHelper.FileName));

        return EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            file,
            dryRun);
    }
}
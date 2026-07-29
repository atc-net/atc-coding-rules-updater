namespace Atc.CodingRules.Updater.Tests;

public sealed class RunSummaryTests
{
    [Theory]
    [InlineData(FileUpdateOutcome.Created, true)]
    [InlineData(FileUpdateOutcome.Updated, true)]
    [InlineData(FileUpdateOutcome.Unchanged, false)]
    [InlineData(FileUpdateOutcome.Skipped, false)]
    public void HasChanges_ReflectsWhetherAnyFileWasWritten(
        FileUpdateOutcome outcome,
        bool expected)
    {
        var summary = new RunSummary();
        summary.Files.Add(new RunFileResult("root", ".editorconfig", "/x/.editorconfig", outcome));

        summary.HasChanges.Should().Be(expected);
    }

    [Fact]
    public void HasChanges_IsFalse_WhenOnlyDriftWasReported()
    {
        // Drift is never applied, so it is not something a re-run would resolve and must not
        // trip --failOnChanges.
        var summary = new RunSummary();
        summary.Files.Add(new RunFileResult("src", "Directory.Build.props", "/x/Directory.Build.props", FileUpdateOutcome.Unchanged));
        summary.Drift.Add(new RunDriftEntry("src", ["Meziantou.Analyzer"], [], []));

        summary.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void MapExitCode_ReturnsSuccess_WhenFailOnChangesIsOff()
    {
        var summary = new RunSummary();
        summary.Files.Add(new RunFileResult("root", ".editorconfig", "/x/.editorconfig", FileUpdateOutcome.Updated));

        RunCommand.MapExitCode(summary, failOnChanges: false)
            .Should().Be(ConsoleExitStatusCodes.Success);
    }

    [Fact]
    public void MapExitCode_ReturnsFailure_WhenFailOnChangesIsOnAndSomethingChanged()
    {
        var summary = new RunSummary();
        summary.Files.Add(new RunFileResult("root", ".editorconfig", "/x/.editorconfig", FileUpdateOutcome.Updated));

        RunCommand.MapExitCode(summary, failOnChanges: true)
            .Should().Be(ConsoleExitStatusCodes.Failure);
    }

    [Fact]
    public void MapExitCode_ReturnsSuccess_WhenFailOnChangesIsOnAndNothingChanged()
    {
        var summary = new RunSummary();
        summary.Files.Add(new RunFileResult("root", ".editorconfig", "/x/.editorconfig", FileUpdateOutcome.Unchanged));

        RunCommand.MapExitCode(summary, failOnChanges: true)
            .Should().Be(ConsoleExitStatusCodes.Success);
    }
}
namespace Atc.CodingRules.Updater.Tests;

public sealed class DirectoryBuildPropsDriftLoggingTests
{
    private readonly ITestOutputHelper testOutput;

    public DirectoryBuildPropsDriftLoggingTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public void LogDrift_LogsNothing_WhenThereIsNoDrift()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        DirectoryBuildPropsHelper.LogDrift(logger, DirectoryBuildPropsDrift.Empty, "root: Directory.Build.props");

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void LogDrift_ReportsSummaryAtInformationLevel()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);
        var drift = new DirectoryBuildPropsDrift(
            ["Meziantou.Analyzer"],
            [],
            ["EnforceCodeStyleInBuild"]);

        DirectoryBuildPropsHelper.LogDrift(logger, drift, "root: Directory.Build.props");

        logger.Entries
            .Should().Contain(x => x.LogLevel == LogLevel.Information
                                   && x.Message.Contains("root: Directory.Build.props", StringComparison.Ordinal)
                                   && x.Message.Contains("2 place(s)", StringComparison.Ordinal));
    }

    [Fact]
    public void LogDrift_NamesEachAddedPackageAndProperty()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);
        var drift = new DirectoryBuildPropsDrift(
            ["Meziantou.Analyzer"],
            ["OldAnalyzer"],
            ["EnforceCodeStyleInBuild"]);

        DirectoryBuildPropsHelper.LogDrift(logger, drift, "root: Directory.Build.props");

        var messages = logger.Entries.Select(x => x.Message).ToList();
        messages.Should().Contain(x => x.Contains("Meziantou.Analyzer", StringComparison.Ordinal));
        messages.Should().Contain(x => x.Contains("OldAnalyzer", StringComparison.Ordinal));
        messages.Should().Contain(x => x.Contains("EnforceCodeStyleInBuild", StringComparison.Ordinal));
    }
}
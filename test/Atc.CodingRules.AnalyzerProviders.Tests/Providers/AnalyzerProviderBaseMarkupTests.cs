namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

/// <summary>
/// The <c>logWithAnsiConsoleMarkup</c> flag exists so that machine-readable output
/// (<c>analyzer-providers collect --json</c>) is not polluted with Spectre markup. These tests
/// pin that every log line honours it, not just the timing line.
/// </summary>
/// <remarks>
/// Uses a local fake provider so <see cref="ProviderCollectingMode.ReCollect"/> runs entirely
/// offline.
/// </remarks>
public sealed class AnalyzerProviderBaseMarkupTests
{
    private readonly ITestOutputHelper testOutput;

    public AnalyzerProviderBaseMarkupTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public async Task CollectBaseRules_EmitsNoSpectreMarkup_WhenMarkupIsDisabled()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);
        var provider = new FakeAnalyzerProvider(logger, logWithAnsiConsoleMarkup: false);

        await provider.CollectBaseRules(ProviderCollectingMode.ReCollect);

        logger.Entries
            .Should().NotContain(x => x.Message.Contains("[green]", StringComparison.Ordinal)
                                      || x.Message.Contains("[yellow]", StringComparison.Ordinal)
                                      || x.Message.Contains("[/]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CollectBaseRules_EmitsSpectreMarkup_WhenMarkupIsEnabled()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);
        var provider = new FakeAnalyzerProvider(logger, logWithAnsiConsoleMarkup: true);

        await provider.CollectBaseRules(ProviderCollectingMode.ReCollect);

        logger.Entries
            .Should().Contain(x => x.Message.Contains("[green]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CollectBaseRules_ReportsSnapshotAge_WhenServingFromLocalCache()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        // Seed the snapshot, then read it back through LocalCache.
        await new FakeAnalyzerProvider(logger, logWithAnsiConsoleMarkup: false)
            .CollectBaseRules(ProviderCollectingMode.ReCollect);

        var provider = new FakeAnalyzerProvider(logger, logWithAnsiConsoleMarkup: false);
        await provider.CollectBaseRules(ProviderCollectingMode.LocalCache);

        logger.Entries
            .Should().Contain(x => x.Message.Contains("cached snapshot from", StringComparison.Ordinal)
                                   && x.Message.Contains(FakeAnalyzerProvider.Name, StringComparison.Ordinal));
    }
}
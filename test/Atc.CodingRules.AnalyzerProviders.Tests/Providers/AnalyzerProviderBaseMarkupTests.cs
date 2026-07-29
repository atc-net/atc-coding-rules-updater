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

    private sealed class FakeAnalyzerProvider : AnalyzerProviderBase
    {
        public FakeAnalyzerProvider(
            ILogger logger,
            bool logWithAnsiConsoleMarkup)
            : base(logger, logWithAnsiConsoleMarkup)
        {
        }

        // Deliberately not a real provider name, so the snapshot this writes to the shared
        // temp folder cannot be picked up by the LocalCache tests of a real provider.
        private static string Name => "Fake.MarkupTestProvider";

        protected override AnalyzerProviderBaseRuleData CreateData()
            => new(Name);

        protected override Task ReCollect(AnalyzerProviderBaseRuleData data)
        {
            ArgumentNullException.ThrowIfNull(data);

            data.Rules.Add(new Rule("FAKE001", "Fake rule", link: string.Empty));

            return Task.CompletedTask;
        }
    }
}
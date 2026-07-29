namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

/// <summary>
/// A provider whose <see cref="ReCollect"/> produces data locally, so
/// <see cref="AnalyzerProviderBase.CollectBaseRules"/> can be exercised without touching the
/// network.
/// </summary>
internal sealed class FakeAnalyzerProvider : AnalyzerProviderBase
{
    public FakeAnalyzerProvider(
        ILogger logger,
        bool logWithAnsiConsoleMarkup)
        : base(logger, logWithAnsiConsoleMarkup)
    {
    }

    /// <summary>
    /// Deliberately not a real provider name, so the snapshot this writes to the shared temp
    /// folder cannot be picked up by the LocalCache tests of a real provider.
    /// </summary>
    public static string Name => "Fake.TestProvider";

    protected override AnalyzerProviderBaseRuleData CreateData()
        => new(Name);

    protected override Task ReCollect(AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.Rules.Add(new Rule("FAKE001", "Fake rule", link: string.Empty));

        return Task.CompletedTask;
    }
}
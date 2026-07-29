namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public class MicrosoftCodeAnalysisNetAnalyzersProviderTests
{
    [Theory]
    [InlineData(ProviderCollectingMode.LocalCache)]
    [InlineData(ProviderCollectingMode.GitHub)]
    [InlineData(ProviderCollectingMode.ReCollect)]
    public async Task CollectBaseRules(
        ProviderCollectingMode providerCollectingMode)
    {
        // Arrange & Act
        var actual = await RetryHelper.ExecuteWithRetryAsync(
            () => new MicrosoftCodeAnalysisNetAnalyzersProvider(NullLogger.Instance).CollectBaseRules(providerCollectingMode),
            x => x.Rules.Count >= 252);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(MicrosoftCodeAnalysisNetAnalyzersProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 252);

        // The catalog mixes "CA", "IDE", and "IL" prefixes — just verify codes look like rule IDs.
        Assert.All(actual.Rules, rule => Assert.Matches("^(CA|IDE|IL)[0-9]+", rule.Code));
        Assert.Contains(actual.Rules, rule => rule.Code.Equals("CA1707", StringComparison.Ordinal));
    }
}
namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class StyleCopAnalyzersProviderTests
{
    [Theory]
    [InlineData(ProviderCollectingMode.LocalCache)]
    [InlineData(ProviderCollectingMode.GitHub)]
    [InlineData(ProviderCollectingMode.ReCollect)]
    public async Task CollectBaseRules(
        ProviderCollectingMode providerCollectingMode)
    {
        AnalyzerProviderBaseRuleData? actual = null;

        await RetryHelper.ExecuteWithRetryAsync(async () =>
        {
            // Arrange
            var provider = new StyleCopAnalyzersProvider(NullLogger.Instance);

            // Act
            actual = await provider.CollectBaseRules(providerCollectingMode);
        });

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(StyleCopAnalyzersProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 198);
        Assert.All(actual.Rules, rule => Assert.StartsWith("SA", rule.Code, StringComparison.Ordinal));
        Assert.Contains(actual.Rules, rule => rule.Code.Equals("SA1600", StringComparison.Ordinal));
    }
}
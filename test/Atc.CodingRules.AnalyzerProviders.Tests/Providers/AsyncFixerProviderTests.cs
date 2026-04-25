namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class AsyncFixerProviderTests
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
            var provider = new AsyncFixerProvider(NullLogger.Instance);

            // Act
            actual = await provider.CollectBaseRules(providerCollectingMode);
        });

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(AsyncFixerProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 5);
        Assert.All(actual.Rules, rule => Assert.StartsWith("AsyncFixer", rule.Code, StringComparison.Ordinal));
        Assert.Contains(actual.Rules, rule => rule.Code.Equals("AsyncFixer01", StringComparison.Ordinal));
    }
}
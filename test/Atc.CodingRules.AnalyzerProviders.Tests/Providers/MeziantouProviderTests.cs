namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class MeziantouProviderTests
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
            () => new MeziantouProvider(NullLogger.Instance).CollectBaseRules(providerCollectingMode),
            x => x.Rules.Count >= 101);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(MeziantouProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 101);
        Assert.All(actual.Rules, rule => Assert.StartsWith("MA", rule.Code, StringComparison.Ordinal));
        Assert.Contains(actual.Rules, rule => rule.Code.Equals("MA0048", StringComparison.Ordinal));
    }
}
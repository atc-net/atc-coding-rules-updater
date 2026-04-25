namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class MicrosoftVisualStudioThreadingAnalyzersProviderTests
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
            var provider = new MicrosoftVisualStudioThreadingAnalyzersProvider(NullLogger.Instance);

            // Act
            actual = await provider.CollectBaseRules(providerCollectingMode);
        });

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(MicrosoftVisualStudioThreadingAnalyzersProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 23);
        Assert.All(actual.Rules, rule => Assert.StartsWith("VSTHRD", rule.Code, StringComparison.Ordinal));
        Assert.Contains(actual.Rules, rule => rule.Code.Equals("VSTHRD100", StringComparison.Ordinal));
    }
}
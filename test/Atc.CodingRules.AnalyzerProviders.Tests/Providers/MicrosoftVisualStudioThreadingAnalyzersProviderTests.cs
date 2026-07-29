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
        // Arrange & Act
        var actual = await RetryHelper.ExecuteWithRetryAsync(
            () => new MicrosoftVisualStudioThreadingAnalyzersProvider(NullLogger.Instance).CollectBaseRules(providerCollectingMode),
            x => x.Rules.Count >= 23);

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
namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class MicrosoftCompilerErrorsProviderTests
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
            () => new MicrosoftCompilerErrorsProvider(NullLogger.Instance).CollectBaseRules(providerCollectingMode),
            x => x.Rules.Count >= 850);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(MicrosoftCompilerErrorsProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 850);
        Assert.All(actual.Rules, rule => Assert.StartsWith("CS", rule.Code, StringComparison.Ordinal));
        Assert.Contains(actual.Rules, rule => rule.Code.Equals("CS4014", StringComparison.Ordinal));
    }
}
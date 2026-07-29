namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class SonarAnalyzerCSharpProviderTests
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
            () => new SonarAnalyzerCSharpProvider(NullLogger.Instance).CollectBaseRules(providerCollectingMode),
            x => x.Rules.Count >= 400);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(SonarAnalyzerCSharpProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 400);

        // The provider strips the "RSPEC-" prefix and the upstream JSON returns numeric IDs.
        // Just verify codes look like rule IDs and a known stable rule (S1118 → "1118") is present.
        Assert.All(actual.Rules, rule => Assert.Matches("^S?[0-9]+", rule.Code));
        Assert.Contains(actual.Rules, rule =>
            rule.Code.Equals("1118", StringComparison.Ordinal)
            || rule.Code.Equals("S1118", StringComparison.Ordinal));
    }
}
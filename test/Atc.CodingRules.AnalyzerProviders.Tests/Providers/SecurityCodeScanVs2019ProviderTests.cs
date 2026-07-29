namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

[Trait(Traits.Category, Traits.Categories.Integration)]
[Trait(Traits.Category, Traits.Categories.SkipWhenLiveUnitTesting)]
public sealed class SecurityCodeScanVs2019ProviderTests
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
            () => new SecurityCodeScanVs2019Provider(NullLogger.Instance).CollectBaseRules(providerCollectingMode),
            x => x.Rules.Count >= 31);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(SecurityCodeScanVs2019Provider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 31);
        Assert.All(actual.Rules, rule => Assert.StartsWith("SCS", rule.Code, StringComparison.Ordinal));
    }
}
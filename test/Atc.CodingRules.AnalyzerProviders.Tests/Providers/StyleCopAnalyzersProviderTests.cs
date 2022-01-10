namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

public class StyleCopAnalyzersProviderTests
{
    [Theory]
    [InlineData(ProviderCollectingMode.LocalCache)]
    [InlineData(ProviderCollectingMode.GitHub)]
    [InlineData(ProviderCollectingMode.NoCache)]
    public async Task CollectBaseRules(ProviderCollectingMode providerCollectingMode)
    {
        // Arrange
        var provider = new StyleCopAnalyzersProvider(NullLogger.Instance);

        // Act
        var actual = await provider.CollectBaseRules(providerCollectingMode);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(StyleCopAnalyzersProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 198);
    }
}
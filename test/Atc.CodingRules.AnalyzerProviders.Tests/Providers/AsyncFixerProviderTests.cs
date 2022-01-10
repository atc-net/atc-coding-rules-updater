namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

public class AsyncFixerProviderTests
{
    [Theory]
    [InlineData(ProviderCollectingMode.LocalCache)]
    [InlineData(ProviderCollectingMode.GitHub)]
    [InlineData(ProviderCollectingMode.NoCache)]
    public async Task CollectBaseRules(ProviderCollectingMode providerCollectingMode)
    {
        // Arrange
        var provider = new AsyncFixerProvider(NullLogger.Instance);

        // Act
        var actual = await provider.CollectBaseRules(providerCollectingMode);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(AsyncFixerProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 5);
    }
}
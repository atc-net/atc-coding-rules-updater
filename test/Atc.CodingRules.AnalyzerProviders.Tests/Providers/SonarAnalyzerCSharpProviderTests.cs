namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

public class SonarAnalyzerCSharpProviderTests
{
    [Theory]
    [InlineData(ProviderCollectingMode.LocalCache)]
    [InlineData(ProviderCollectingMode.GitHub)]
    [InlineData(ProviderCollectingMode.ReCollect)]
    public async Task CollectBaseRules(ProviderCollectingMode providerCollectingMode)
    {
        // Arrange
        var provider = new SonarAnalyzerCSharpProvider(NullLogger.Instance);

        // Act
        var actual = await provider.CollectBaseRules(providerCollectingMode);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(SonarAnalyzerCSharpProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 400);
    }
}
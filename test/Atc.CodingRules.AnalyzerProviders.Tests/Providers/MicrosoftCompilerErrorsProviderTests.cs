namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers;

public class MicrosoftCompilerErrorsProviderTests
{
    [Theory]
    [InlineData(ProviderCollectingMode.LocalCache)]
    [InlineData(ProviderCollectingMode.GitHub)]
    [InlineData(ProviderCollectingMode.NoCache)]
    public async Task CollectBaseRules(ProviderCollectingMode providerCollectingMode)
    {
        // Arrange
        var provider = new MicrosoftCompilerErrorsProvider(NullLogger.Instance);

        // Act
        var actual = await provider.CollectBaseRules(providerCollectingMode);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Name);
        Assert.Equal(MicrosoftCompilerErrorsProvider.Name, actual.Name);
        Assert.NotNull(actual.Rules);
        Assert.True(actual.Rules.Count >= 935);
    }
}
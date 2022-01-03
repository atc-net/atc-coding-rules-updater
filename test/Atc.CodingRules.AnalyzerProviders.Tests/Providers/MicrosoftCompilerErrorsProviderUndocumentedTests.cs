using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Providers;
using Xunit;

namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers
{
    public class MicrosoftCompilerErrorsProviderUndocumentedTests
    {
        [Theory]
        [InlineData(ProviderCollectingMode.LocalCache)]
        [InlineData(ProviderCollectingMode.GitHub)]
        [InlineData(ProviderCollectingMode.ReCollect)]
        public async Task CollectBaseRules(ProviderCollectingMode providerCollectingMode)
        {
            // Arrange
            var provider = new MicrosoftCompilerErrorsProviderUndocumented();

            // Act
            var actual = await provider.CollectBaseRules(providerCollectingMode);

            // Assert
            Assert.NotNull(actual);
            Assert.NotNull(actual.Name);
            Assert.Equal(MicrosoftCompilerErrorsProviderUndocumented.Name, actual.Name);
            Assert.NotNull(actual.Rules);
            Assert.True(actual.Rules.Count >= 46);
        }
    }
}
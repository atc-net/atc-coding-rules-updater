using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Providers;
using Xunit;

namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers
{
    public class AsyncifyProviderTests
    {
        [Fact]
        public async Task CollectBaseRules()
        {
            // Arrange
            var provider = new AsyncifyProvider();

            // Act
            var actual = await provider.CollectBaseRules();

            // Assert
            Assert.NotNull(actual);
            Assert.NotNull(actual.Name);
            Assert.Equal("Asyncify", actual.Name);
            Assert.NotNull(actual.Rules);
            Assert.True(actual.Rules.Count >= 2);
        }
    }
}
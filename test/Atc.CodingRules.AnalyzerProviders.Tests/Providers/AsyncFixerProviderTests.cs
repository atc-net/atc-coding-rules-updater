using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Providers;
using Xunit;

namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers
{
    public class AsyncFixerProviderTests
    {
        [Fact]
        public async Task CollectBaseRules()
        {
            // Arrange
            var provider = new AsyncFixerProvider();

            // Act
            var actual = await provider.CollectBaseRules();

            // Assert
            Assert.NotNull(actual);
            Assert.NotNull(actual.Name);
            Assert.Equal("AsyncFixer", actual.Name);
            Assert.NotNull(actual.Rules);
            Assert.True(actual.Rules.Count >= 5);
        }
    }
}
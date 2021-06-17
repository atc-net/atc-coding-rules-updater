using System.Threading.Tasks;
using Atc.CodingRules.AnalyzerProviders.Providers;
using Xunit;

namespace Atc.CodingRules.AnalyzerProviders.Tests.Providers
{
    public class SonarAnalyzerCSharpProviderTests
    {
        [Fact]
        public async Task CollectBaseRules()
        {
            // Arrange
            var provider = new SonarAnalyzerCSharpProvider();

            // Act
            var actual = await provider.CollectBaseRules();

            // Assert
            Assert.NotNull(actual);
            Assert.NotNull(actual.Name);
            Assert.Equal("SonarAnalyzer.CSharp", actual.Name);
            Assert.NotNull(actual.Rules);
            ////Assert.True(actual.Rules.Count >= 409); // Only works locally (and not in github action)
        }
    }
}
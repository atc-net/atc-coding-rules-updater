namespace Atc.CodingRules.AnalyzerProviders.Tests;

public sealed class AnalyzerProviderCollectorTests
{
    /// <summary>
    /// Provider name resolution used to be a hand-maintained switch over concrete types, where a
    /// missing arm silently degraded to <c>GetType().Name</c> and broke
    /// <c>--includeProviders</c> / <c>--excludeProviders</c> matching. Reflecting over every
    /// concrete provider means a newly added one cannot slip through.
    /// </summary>
    [Fact]
    public void ProviderName_MatchesTheStaticNameProperty_ForEveryProvider()
    {
        var providerTypes = typeof(AnalyzerProviderBase).Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && typeof(AnalyzerProviderBase).IsAssignableFrom(x))
            .ToList();

        providerTypes.Should().HaveCountGreaterThan(10);

        foreach (var providerType in providerTypes)
        {
            var instance = (AnalyzerProviderBase)Activator.CreateInstance(
                providerType,
                NullLogger.Instance,
                false)!;

            var staticName = (string)providerType
                .GetProperty("Name", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;

            instance.ProviderName.Should().Be(staticName);
        }
    }
}
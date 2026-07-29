namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// The ATC API caches resolved versions in an in-memory cache for 12 hours, so a package
/// published inside that window is served stale. The endpoint supports <c>invalidateCache=true</c>
/// (the release workflow uses it), but the client had no way to ask for it.
/// </summary>
public sealed class AtcApiNugetClientHelperTests
{
    [Fact]
    public void BuildPackageUri_OmitsInvalidateCache_ByDefault()
    {
        var actual = AtcApiNugetClientHelper.BuildPackageUri("Meziantou.Analyzer", forceRefresh: false);

        actual.Query.Should().Be("?packageId=Meziantou.Analyzer");
    }

    [Fact]
    public void BuildPackageUri_AddsInvalidateCache_WhenForcingRefresh()
    {
        var actual = AtcApiNugetClientHelper.BuildPackageUri("Meziantou.Analyzer", forceRefresh: true);

        actual.Query.Should().Be("?packageId=Meziantou.Analyzer&invalidateCache=true");
    }

    [Fact]
    public void BuildPackageUri_EscapesPackageId()
    {
        var actual = AtcApiNugetClientHelper.BuildPackageUri("Some Package&x", forceRefresh: false);

        actual.Query.Should().NotContain(" ");
        actual.Query.Should().Contain("Some%20Package%26x");
    }
}
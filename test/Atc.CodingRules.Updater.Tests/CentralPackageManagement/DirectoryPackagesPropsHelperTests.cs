namespace Atc.CodingRules.Updater.Tests.CentralPackageManagement;

/// <summary>
/// Maintains the distribution-owned entries of an existing <c>Directory.Packages.props</c>.
/// Versions come from a fake resolver, so these tests never touch the network.
/// </summary>
public sealed class DirectoryPackagesPropsHelperTests
{
    private static readonly string[] OwnedIds =
    [
        "AsyncFixer",
        "Meziantou.Analyzer",
        "StyleCop.Analyzers",
        "Atc.Test",
        "Microsoft.NET.Test.Sdk",
    ];

    [Fact]
    public void Apply_BumpsOwnedPackageVersion_WithinSameMajor()
    {
        var content = ConsumerPackagesProps();

        var actual = DirectoryPackagesPropsHelper.Apply(content, [], OwnedIds, Resolver(("Atc.Test", "3.4.0")));

        actual.Content.Should().Contain("<PackageVersion Include=\"Atc.Test\" Version=\"3.4.0\" />");
        actual.Bumps.Should().ContainSingle().Which.PackageId.Should().Be("Atc.Test");
    }

    [Fact]
    public void Apply_BumpsOwnedGlobalPackageReference()
    {
        var content = ConsumerPackagesProps();

        var actual = DirectoryPackagesPropsHelper.Apply(content, [], OwnedIds, Resolver(("AsyncFixer", "2.1.0")));

        actual.Content.Should().Contain("<GlobalPackageReference Include=\"AsyncFixer\" Version=\"2.1.0\" />");
    }

    [Fact]
    public void Apply_ChangesOnlyTheVersionText_AndPreservesFormatting()
    {
        var content = ConsumerPackagesProps();

        var actual = DirectoryPackagesPropsHelper.Apply(content, [], OwnedIds, Resolver(("Atc.Test", "3.4.0")));

        actual.Content.Should().Be(content.Replace(
            "<PackageVersion Include=\"Atc.Test\" Version=\"3.1.1\" />",
            "<PackageVersion Include=\"Atc.Test\" Version=\"3.4.0\" />",
            StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("NSubstitute", "5.9.0")]
    [InlineData("System.Security.Cryptography.Xml", "10.0.99")]
    [InlineData("Azure.Identity", "1.99.0")]
    public void Apply_NeverTouchesPackagesTheDistributionDoesNotOwn(
        string packageId,
        string newerVersion)
    {
        var content = ConsumerPackagesProps();

        var actual = DirectoryPackagesPropsHelper.Apply(content, [], OwnedIds, Resolver((packageId, newerVersion)));

        actual.Content.Should().Be(content);
        actual.Bumps.Should().BeEmpty();
    }

    [Fact]
    public void Apply_DoesNotBumpAcrossMajorVersion()
    {
        var content = ConsumerPackagesProps();

        var actual = DirectoryPackagesPropsHelper.Apply(content, [], OwnedIds, Resolver(("Meziantou.Analyzer", "4.0.0")));

        actual.Content.Should().Be(content);
    }

    [Fact]
    public void Apply_SkipsPrereleaseVersions()
    {
        var content = ConsumerPackagesProps();

        var actual = DirectoryPackagesPropsHelper.Apply(content, [], OwnedIds, Resolver(("StyleCop.Analyzers", "1.3.0")));

        actual.Content.Should().Be(content);
    }

    [Fact]
    public void Apply_SkipsConditionalEntries()
    {
        const string content = "<Project><ItemGroup Condition=\"'$(X)' == 'y'\">\n  <PackageVersion Include=\"Atc.Test\" Version=\"3.1.1\" />\n</ItemGroup></Project>";

        var actual = DirectoryPackagesPropsHelper.Apply(content, [], OwnedIds, Resolver(("Atc.Test", "3.4.0")));

        actual.Content.Should().Be(content);
    }

    [Fact]
    public void Apply_DoesNotBump_WhenNoResolverIsGiven()
    {
        var content = ConsumerPackagesProps();

        var actual = DirectoryPackagesPropsHelper.Apply(content, [], OwnedIds, resolveLatestVersion: null);

        actual.Content.Should().Be(content);
    }

    [Fact]
    public void Apply_AppendsMissingRequiredEntriesUnderTheirLabel()
    {
        var content = ConsumerPackagesProps();
        CpmRequiredEntry[] required =
        [
            new(PackageItemType.PackageVersion, "Microsoft.Testing.Extensions.TrxReport", "2.4.1", "atc-coding-rules: test"),
            new(PackageItemType.GlobalPackageReference, "SonarAnalyzer.CSharp", "10.34.0.3385", "atc-coding-rules: Code Analyzers"),
        ];

        var actual = DirectoryPackagesPropsHelper.Apply(content, required, OwnedIds, resolveLatestVersion: null);

        var items = PackageItemParser.Parse(actual.Content)!;
        items.Should().ContainSingle(x => x.Id == "Microsoft.Testing.Extensions.TrxReport" &&
                                          x.ItemType == PackageItemType.PackageVersion &&
                                          x.ItemGroupLabel == "atc-coding-rules: test");
        items.Should().ContainSingle(x => x.Id == "SonarAnalyzer.CSharp" &&
                                          x.ItemType == PackageItemType.GlobalPackageReference);
        actual.Appended.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_NeverDuplicatesAnEntryThatAlreadyExists_EvenInAnotherGroupOrCase()
    {
        var content = ConsumerPackagesProps();
        CpmRequiredEntry[] required =
        [
            new(PackageItemType.PackageVersion, "atc.test", "2.0.16", "atc-coding-rules: test"),
            new(PackageItemType.GlobalPackageReference, "AsyncFixer", "2.1.0", "atc-coding-rules: Code Analyzers"),
        ];

        var actual = DirectoryPackagesPropsHelper.Apply(content, required, OwnedIds, resolveLatestVersion: null);

        actual.Content.Should().Be(content);
        actual.Appended.Should().BeEmpty();
    }

    [Fact]
    public void Apply_IsIdempotent()
    {
        var content = ConsumerPackagesProps();
        CpmRequiredEntry[] required =
        [
            new(PackageItemType.PackageVersion, "Microsoft.Testing.Extensions.TrxReport", "2.4.1", "atc-coding-rules: test"),
        ];
        var resolver = Resolver(("Atc.Test", "3.4.0"), ("Microsoft.Testing.Extensions.TrxReport", "2.4.1"));

        var first = DirectoryPackagesPropsHelper.Apply(content, required, OwnedIds, resolver);
        var second = DirectoryPackagesPropsHelper.Apply(first.Content, required, OwnedIds, resolver);

        second.Content.Should().Be(first.Content);
        second.Bumps.Should().BeEmpty();
        second.Appended.Should().BeEmpty();
    }

    private static string ConsumerPackagesProps()
        => TestFixtures.Read("TestFilesCpm/Consumer/Directory.Packages.props.xml");

    private static Func<string, Version?> Resolver(
        params (string PackageId, string Version)[] versions)
        => packageId => versions
            .Where(x => x.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase))
            .Select(x => new Version(x.Version))
            .FirstOrDefault();
}
namespace Atc.CodingRules.Updater.Tests.MsBuild;

/// <summary>
/// The editor changes hand-written MSBuild files, so every test pins that only the targeted
/// span changes: indentation, comments and line endings must survive untouched.
/// </summary>
public sealed class MsBuildFileTextEditorTests
{
    [Fact]
    public void SetVersion_ReplacesVersionAttribute_AndLeavesEverythingElseUntouched()
    {
        const string content = "<Project>\r\n\t<ItemGroup>\r\n\t<!-- keep me -->\r\n\t<PackageVersion Include=\"Atc\" Version=\"3.0.1\" />\r\n\t</ItemGroup>\r\n</Project>";

        var actual = MsBuildFileTextEditor.SetVersion(content, Item(content, "Atc"), "3.0.9");

        actual.Should().Be(content.Replace("3.0.1", "3.0.9", StringComparison.Ordinal));
    }

    [Fact]
    public void SetVersion_HandlesVersionBeforeInclude()
    {
        const string content = "<Project><ItemGroup>\n  <GlobalPackageReference Version=\"2.1.0\" Include=\"AsyncFixer\" />\n</ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.SetVersion(content, Item(content, "AsyncFixer"), "2.2.0");

        actual.Should().Contain("<GlobalPackageReference Version=\"2.2.0\" Include=\"AsyncFixer\" />");
    }

    [Fact]
    public void SetVersion_DoesNotTouchVersionOverride()
    {
        const string content = "<Project><ItemGroup>\n  <PackageReference Include=\"Atc\" VersionOverride=\"1.0.0\" Version=\"3.0.1\" />\n</ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.SetVersion(content, Item(content, "Atc"), "3.0.9");

        actual.Should().Contain("VersionOverride=\"1.0.0\" Version=\"3.0.9\"");
    }

    [Fact]
    public void SetVersion_ReplacesVersionChildElement()
    {
        const string content = "<Project><ItemGroup>\n  <PackageReference Include=\"Atc\">\n    <Version>3.0.1</Version>\n  </PackageReference>\n</ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.SetVersion(content, Item(content, "Atc"), "3.0.9");

        actual.Should().Be(content.Replace("3.0.1", "3.0.9", StringComparison.Ordinal));
    }

    [Fact]
    public void SetVersion_TargetsTheGivenItem_WhenSeveralItemsShareALine()
    {
        const string content = "<Project><ItemGroup><PackageVersion Include=\"A\" Version=\"1.0.0\" /><PackageVersion Include=\"B\" Version=\"1.0.0\" /></ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.SetVersion(content, Item(content, "B"), "2.0.0");

        actual.Should().Be("<Project><ItemGroup><PackageVersion Include=\"A\" Version=\"1.0.0\" /><PackageVersion Include=\"B\" Version=\"2.0.0\" /></ItemGroup></Project>");
    }

    [Fact]
    public void RemoveVersion_RemovesVersionAttribute()
    {
        const string content = "<Project><ItemGroup>\n  <PackageReference Include=\"Atc\" Version=\"3.0.1\" PrivateAssets=\"All\" />\n</ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.RemoveVersion(content, Item(content, "Atc"));

        actual.Should().Contain("<PackageReference Include=\"Atc\" PrivateAssets=\"All\" />");
    }

    [Fact]
    public void RemoveVersion_RemovesVersionChildElement_AndKeepsOtherMetadata()
    {
        const string content = "<Project><ItemGroup>\n  <PackageReference Include=\"xunit.runner.visualstudio\">\n    <Version>3.1.5</Version>\n    <PrivateAssets>all</PrivateAssets>\n  </PackageReference>\n</ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.RemoveVersion(content, Item(content, "xunit.runner.visualstudio"));

        actual.Should().Be("<Project><ItemGroup>\n  <PackageReference Include=\"xunit.runner.visualstudio\">\n    <PrivateAssets>all</PrivateAssets>\n  </PackageReference>\n</ItemGroup></Project>");
    }

    [Fact]
    public void RemoveVersion_CollapsesElement_WhenVersionWasTheOnlyChild()
    {
        const string content = "<Project><ItemGroup>\n  <PackageReference Include=\"Atc\">\n    <Version>3.0.1</Version>\n  </PackageReference>\n</ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.RemoveVersion(content, Item(content, "Atc"));

        actual.Should().Be("<Project><ItemGroup>\n  <PackageReference Include=\"Atc\" />\n</ItemGroup></Project>");
    }

    [Fact]
    public void ReplaceVersionWithVersionOverride_RenamesAttribute()
    {
        const string content = "<Project><ItemGroup>\n  <PackageReference Include=\"Atc\" Version=\"3.0.1\" />\n</ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.ReplaceVersionWithVersionOverride(content, Item(content, "Atc"));

        actual.Should().Contain("<PackageReference Include=\"Atc\" VersionOverride=\"3.0.1\" />");
    }

    [Fact]
    public void ReplaceVersionWithVersionOverride_RenamesChildElement()
    {
        const string content = "<Project><ItemGroup>\n  <PackageReference Include=\"Atc\">\n    <Version>3.0.1</Version>\n  </PackageReference>\n</ItemGroup></Project>";

        var actual = MsBuildFileTextEditor.ReplaceVersionWithVersionOverride(content, Item(content, "Atc"));

        actual.Should().Contain("<VersionOverride>3.0.1</VersionOverride>");
    }

    [Fact]
    public void ReplaceItemGroup_ReplacesLabelledGroupWithComment()
    {
        const string content = "<Project>\r\n\r\n  <!-- Shared code analyzers used for all projects in the solution -->\r\n  <ItemGroup Label=\"Code Analyzers\">\r\n    <PackageReference Include=\"AsyncFixer\" Version=\"2.1.0\" PrivateAssets=\"All\" />\r\n  </ItemGroup>\r\n\r\n</Project>";

        var actual = MsBuildFileTextEditor.ReplaceItemGroup(content, "Code Analyzers", ["Declared in Directory.Packages.props."]);

        actual.Should().Be("<Project>\r\n\r\n  <!-- Shared code analyzers used for all projects in the solution -->\r\n  <!--\r\n    Declared in Directory.Packages.props.\r\n  -->\r\n\r\n</Project>");
    }

    [Fact]
    public void ReplaceItemGroup_ReturnsContentUnchanged_WhenLabelIsMissing()
    {
        const string content = "<Project>\n  <ItemGroup Label=\"Other\" />\n</Project>";

        var actual = MsBuildFileTextEditor.ReplaceItemGroup(content, "Code Analyzers", ["x"]);

        actual.Should().Be(content);
    }

    [Fact]
    public void AppendItems_AddsLabelledGroupBeforeProjectEnd_UsingFileIndentationAndNewlines()
    {
        const string content = "<Project>\r\n\t<ItemGroup>\r\n\t\t<PackageVersion Include=\"Atc\" Version=\"3.0.1\" />\r\n\t</ItemGroup>\r\n</Project>";

        var actual = MsBuildFileTextEditor.AppendItems(
            content,
            "atc-coding-rules: test",
            ["<PackageVersion Include=\"Atc.Test\" Version=\"3.1.1\" />"]);

        actual.Should().Be("<Project>\r\n\t<ItemGroup>\r\n\t\t<PackageVersion Include=\"Atc\" Version=\"3.0.1\" />\r\n\t</ItemGroup>\r\n\r\n\t<ItemGroup Label=\"atc-coding-rules: test\">\r\n\t\t<PackageVersion Include=\"Atc.Test\" Version=\"3.1.1\" />\r\n\t</ItemGroup>\r\n\r\n</Project>");
    }

    [Fact]
    public void AppendItems_AddsToExistingLabelledGroup()
    {
        const string content = "<Project>\n  <ItemGroup Label=\"atc-coding-rules: test\">\n    <PackageVersion Include=\"Atc.Test\" Version=\"3.1.1\" />\n  </ItemGroup>\n</Project>";

        var actual = MsBuildFileTextEditor.AppendItems(
            content,
            "atc-coding-rules: test",
            ["<PackageVersion Include=\"xunit.v3\" Version=\"4.0.1\" />"]);

        actual.Should().Be("<Project>\n  <ItemGroup Label=\"atc-coding-rules: test\">\n    <PackageVersion Include=\"Atc.Test\" Version=\"3.1.1\" />\n    <PackageVersion Include=\"xunit.v3\" Version=\"4.0.1\" />\n  </ItemGroup>\n</Project>");
    }

    [Fact]
    public void AppendItems_ProducesParseableXml_ForSelfClosingProject()
    {
        const string content = "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>";

        var actual = MsBuildFileTextEditor.AppendItems(
            content,
            "atc-coding-rules: Code Analyzers",
            ["<GlobalPackageReference Include=\"AsyncFixer\" Version=\"2.1.0\" />"]);

        PackageItemParser.Parse(actual).Should().ContainSingle().Which.Id.Should().Be("AsyncFixer");
    }

    private static PackageItem Item(
        string content,
        string id)
        => PackageItemParser.Parse(content)!.Single(x => x.Id == id);
}
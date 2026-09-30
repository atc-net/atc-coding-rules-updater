namespace Atc.CodingRules.Updater.Tests.MsBuild;

public sealed class PackageItemParserTests
{
    [Fact]
    public void Parse_ReadsIdAndVersion_FromPackageReferenceAttributes()
    {
        const string content = """
            <Project>
              <ItemGroup>
                <PackageReference Include="AsyncFixer" Version="2.1.0" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle();
        actual![0].ItemType.Should().Be(PackageItemType.PackageReference);
        actual[0].Id.Should().Be("AsyncFixer");
        actual[0].Version.Should().Be("2.1.0");
        actual[0].IsVersionChildElement.Should().BeFalse();
    }

    [Fact]
    public void Parse_ReadsItems_WhenFileHasMsBuildNamespace()
    {
        const string content = """
            <?xml version="1.0" encoding="utf-8"?>
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemGroup>
                <PackageReference Include="AsyncFixer" Version="2.1.0" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle().Which.Id.Should().Be("AsyncFixer");
    }

    [Fact]
    public void Parse_ReadsVersion_FromChildElement()
    {
        const string content = """
            <Project>
              <ItemGroup>
                <PackageReference Include="AsyncFixer">
                  <Version>2.1.0</Version>
                </PackageReference>
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle();
        actual![0].Version.Should().Be("2.1.0");
        actual[0].IsVersionChildElement.Should().BeTrue();
    }

    [Fact]
    public void Parse_ReturnsNullVersion_WhenReferenceIsVersionless()
    {
        const string content = """
            <Project>
              <ItemGroup>
                <PackageReference Include="AsyncFixer" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle().Which.Version.Should().BeNull();
    }

    [Fact]
    public void Parse_ReadsPackageVersionAndGlobalPackageReference()
    {
        const string content = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Atc" Version="3.0.186" />
                <GlobalPackageReference Include="AsyncFixer" Version="2.1.0" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().HaveCount(2);
        actual![0].ItemType.Should().Be(PackageItemType.PackageVersion);
        actual[0].Id.Should().Be("Atc");
        actual[1].ItemType.Should().Be(PackageItemType.GlobalPackageReference);
        actual[1].Id.Should().Be("AsyncFixer");
    }

    [Fact]
    public void Parse_CapturesItemGroupLabel()
    {
        const string content = """
            <Project>
              <ItemGroup Label="Code Analyzers">
                <PackageReference Include="AsyncFixer" Version="2.1.0" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle().Which.ItemGroupLabel.Should().Be("Code Analyzers");
    }

    [Fact]
    public void Parse_MarksItemConditional_WhenItemHasCondition()
    {
        const string content = """
            <Project>
              <ItemGroup>
                <PackageReference Include="AsyncFixer" Version="2.1.0" Condition="'$(TargetFramework)' == 'net10.0'" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle().Which.IsConditional.Should().BeTrue();
    }

    [Fact]
    public void Parse_MarksItemConditional_WhenItemGroupHasCondition()
    {
        const string content = """
            <Project>
              <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
                <PackageReference Include="AsyncFixer" Version="2.1.0" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle().Which.IsConditional.Should().BeTrue();
    }

    [Fact]
    public void Parse_ReadsIdFromUpdate_AndMarksIsUpdate()
    {
        const string content = """
            <Project>
              <ItemGroup>
                <PackageReference Update="AsyncFixer" Version="2.1.0" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle();
        actual![0].Id.Should().Be("AsyncFixer");
        actual[0].IsUpdate.Should().BeTrue();
    }

    [Fact]
    public void Parse_ReadsVersionOverrideAndIsImplicitlyDefined()
    {
        const string content = """
            <Project>
              <ItemGroup>
                <PackageReference Include="AsyncFixer" VersionOverride="1.6.0" />
                <PackageReference Include="Microsoft.NETCore.App" Version="9.0.0" IsImplicitlyDefined="true" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().HaveCount(2);
        actual![0].VersionOverride.Should().Be("1.6.0");
        actual[1].IsImplicitlyDefined.Should().BeTrue();
    }

    [Fact]
    public void Parse_ReportsLineNumberOfElement()
    {
        const string content = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Atc" Version="3.0.186" />
              </ItemGroup>
            </Project>
            """;

        var actual = PackageItemParser.Parse(content);

        actual.Should().ContainSingle().Which.LineNumber.Should().Be(3);
    }

    [Fact]
    public void Parse_ReturnsNull_WhenContentIsNotXml()
    {
        var actual = PackageItemParser.Parse("this is not xml");

        actual.Should().BeNull();
    }
}
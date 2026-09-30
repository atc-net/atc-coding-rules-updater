namespace Atc.CodingRules.Updater.Tests.CentralPackageManagement;

public sealed class CentralPackageManagementHelperTests
{
    private const string PackagesPropsWithFlag = """
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
          </PropertyGroup>
          <ItemGroup>
            <PackageVersion Include="Atc" Version="3.0.186" />
          </ItemGroup>
          <ItemGroup Label="Code Analyzers">
            <GlobalPackageReference Include="AsyncFixer" Version="2.1.0" />
          </ItemGroup>
        </Project>
        """;

    [Fact]
    public void Detect_IsNotEnabled_WhenRepositoryHasNoPackagesProps()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Build.props", "<Project />");

        var actual = CentralPackageManagementHelper.Detect(workspace.Root);

        actual.IsEnabled.Should().BeFalse();
        actual.HasPackagesPropsWithoutFlag.Should().BeFalse();
    }

    [Fact]
    public void Detect_IsEnabled_WhenPackagesPropsSetsFlag()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesPropsWithFlag);

        var actual = CentralPackageManagementHelper.Detect(workspace.Root);

        actual.IsEnabled.Should().BeTrue();
        actual.PackagesPropsFile!.Name.Should().Be("Directory.Packages.props");
    }

    [Fact]
    public void Detect_ReadsPackageVersionAndGlobalPackageReferenceEntries()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesPropsWithFlag);

        var actual = CentralPackageManagementHelper.Detect(workspace.Root);

        actual.Entries.Select(x => x.Id).Should().Equal("Atc", "AsyncFixer");
        actual.HasEntry("asyncfixer").Should().BeTrue();
    }

    [Fact]
    public void Detect_IsEnabled_WhenDirectoryBuildPropsSetsFlag()
    {
        const string buildProps = """
            <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
            </Project>
            """;

        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Build.props", buildProps);
        workspace.WriteFile("Directory.Packages.props", "<Project />");

        var actual = CentralPackageManagementHelper.Detect(workspace.Root);

        actual.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Detect_IsNotEnabled_AndReportsIt_WhenPackagesPropsHasNoFlag()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", "<Project><ItemGroup><PackageVersion Include=\"Atc\" Version=\"1.0.0\" /></ItemGroup></Project>");

        var actual = CentralPackageManagementHelper.Detect(workspace.Root);

        actual.IsEnabled.Should().BeFalse();
        actual.HasPackagesPropsWithoutFlag.Should().BeTrue();
    }

    [Fact]
    public void Detect_IsNotEnabled_WhenFlagIsFalse()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile(
            "Directory.Packages.props",
            "<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>");

        var actual = CentralPackageManagementHelper.Detect(workspace.Root);

        actual.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Detect_IsNotEnabled_WhenModeIsDisabled()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesPropsWithFlag);

        var actual = CentralPackageManagementHelper.Detect(workspace.Root, CentralPackageManagementMode.Disabled);

        actual.IsEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData("6.0.400", false)]
    [InlineData("7.0.100", true)]
    [InlineData("10.0.100", true)]
    public void Detect_ReportsGlobalPackageReferenceSupport_FromGlobalJsonSdkVersion(
        string sdkVersion,
        bool expected)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesPropsWithFlag);
        workspace.WriteFile("global.json", $$"""{ "sdk": { "version": "{{sdkVersion}}" } }""");

        var actual = CentralPackageManagementHelper.Detect(workspace.Root);

        actual.SupportsGlobalPackageReference.Should().Be(expected);
    }

    [Fact]
    public void Detect_AssumesGlobalPackageReferenceSupport_WhenGlobalJsonPinsNoVersion()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesPropsWithFlag);
        workspace.WriteFile("global.json", """{ "sdk": { "rollForward": "latestMajor" } }""");

        var actual = CentralPackageManagementHelper.Detect(workspace.Root);

        actual.SupportsGlobalPackageReference.Should().BeTrue();
    }
}
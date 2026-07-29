namespace Atc.CodingRules.Updater.Tests;

public sealed class DirectoryBuildPropsDriftTests
{
    private const string LocalContent = """
        <Project>
          <PropertyGroup>
            <OrganizationName>Acme</OrganizationName>
            <RepositoryName>my-repo</RepositoryName>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="AsyncFixer" Version="1.6.0" PrivateAssets="All" />
            <PackageReference Include="SonarAnalyzer.CSharp" Version="10.20.0.1" PrivateAssets="All" />
          </ItemGroup>
        </Project>
        """;

    [Fact]
    public void GetDrift_ListsPackageAddedUpstream()
    {
        const string upstream = """
            <Project>
              <PropertyGroup>
                <OrganizationName>Acme</OrganizationName>
                <RepositoryName>my-repo</RepositoryName>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="AsyncFixer" Version="1.6.0" PrivateAssets="All" />
                <PackageReference Include="SonarAnalyzer.CSharp" Version="10.30.0.1" PrivateAssets="All" />
                <PackageReference Include="Meziantou.Analyzer" Version="3.0.135" PrivateAssets="All" />
              </ItemGroup>
            </Project>
            """;

        var actual = DirectoryBuildPropsHelper.GetDrift(upstream, LocalContent);

        actual.PackageReferencesOnlyUpstream.Should().Equal("Meziantou.Analyzer");
        actual.PackageReferencesOnlyLocal.Should().BeEmpty();
    }

    [Fact]
    public void GetDrift_ListsPackageRemovedUpstream()
    {
        const string upstream = """
            <Project>
              <ItemGroup>
                <PackageReference Include="AsyncFixer" Version="1.6.0" PrivateAssets="All" />
              </ItemGroup>
            </Project>
            """;

        var actual = DirectoryBuildPropsHelper.GetDrift(upstream, LocalContent);

        actual.PackageReferencesOnlyLocal.Should().Equal("SonarAnalyzer.CSharp");
        actual.PackageReferencesOnlyUpstream.Should().BeEmpty();
    }

    [Fact]
    public void GetDrift_ListsPropertyAddedUpstream()
    {
        const string upstream = """
            <Project>
              <PropertyGroup>
                <OrganizationName>Acme</OrganizationName>
                <RepositoryName>my-repo</RepositoryName>
                <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="AsyncFixer" Version="1.6.0" PrivateAssets="All" />
                <PackageReference Include="SonarAnalyzer.CSharp" Version="10.20.0.1" PrivateAssets="All" />
              </ItemGroup>
            </Project>
            """;

        var actual = DirectoryBuildPropsHelper.GetDrift(upstream, LocalContent);

        actual.PropertiesOnlyUpstream.Should().Equal("EnforceCodeStyleInBuild");
    }

    [Fact]
    public void GetDrift_IgnoresValueDifferences_BecauseLocalValuesAreIntentional()
    {
        // Same property and package names throughout; only the values differ. The local
        // OrganizationName and the older Sonar version must not be reported as drift.
        const string upstream = """
            <Project>
              <PropertyGroup>
                <OrganizationName><!-- insert organization name here --></OrganizationName>
                <RepositoryName><!-- insert repository name here --></RepositoryName>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="AsyncFixer" Version="1.6.0" PrivateAssets="All" />
                <PackageReference Include="SonarAnalyzer.CSharp" Version="10.30.0.1" PrivateAssets="All" />
              </ItemGroup>
            </Project>
            """;

        var actual = DirectoryBuildPropsHelper.GetDrift(upstream, LocalContent);

        actual.HasDrift.Should().BeFalse();
    }

    [Fact]
    public void GetDrift_ReturnsEmpty_WhenContentIsNotValidXml()
    {
        var actual = DirectoryBuildPropsHelper.GetDrift("<Project>", LocalContent);

        actual.HasDrift.Should().BeFalse();
    }
}
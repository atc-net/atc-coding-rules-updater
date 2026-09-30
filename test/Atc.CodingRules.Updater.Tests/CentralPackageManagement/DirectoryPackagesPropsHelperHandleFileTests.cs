namespace Atc.CodingRules.Updater.Tests.CentralPackageManagement;

public sealed class DirectoryPackagesPropsHelperHandleFileTests
{
    private const string PackagesProps = """
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
          </PropertyGroup>
          <ItemGroup>
            <PackageVersion Include="Atc.Test" Version="3.1.1" />
          </ItemGroup>
        </Project>
        """;

    private static readonly string[] OwnedIds = ["Atc.Test"];

    private readonly ITestOutputHelper testOutput;

    public DirectoryPackagesPropsHelperHandleFileTests(
        ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public void HandleFile_WritesBumpsAndAppends_AndReportsUpdated()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        using var logger = testOutput.BuildLogger();
        CpmRequiredEntry[] required = [new(PackageItemType.PackageVersion, "xunit.v3", "4.0.1", "atc-coding-rules: test")];

        var actual = DirectoryPackagesPropsHelper.HandleFile(
            logger,
            CentralPackageManagementHelper.Detect(workspace.Root),
            required,
            OwnedIds,
            _ => new Version(3, 4, 0));

        actual.Outcome.Should().Be(FileUpdateOutcome.Updated);
        actual.PackageBumps.Should().ContainSingle().Which.NewestVersion.Should().Be(new Version(3, 4, 0));
        var written = workspace.ReadFile("Directory.Packages.props");
        written.Should().Contain("Include=\"Atc.Test\" Version=\"3.4.0\"").And.Contain("Include=\"xunit.v3\" Version=\"4.0.1\"");
    }

    [Fact]
    public void HandleFile_KeepsAByteOrderMark_WhenBumping()
    {
        using var workspace = new TestWorkspace();
        var file = Path.Combine(workspace.Root.FullName, "Directory.Packages.props");
        File.WriteAllBytes(file, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(PackagesProps)]);
        using var logger = testOutput.BuildLogger();

        DirectoryPackagesPropsHelper.HandleFile(
            logger,
            CentralPackageManagementHelper.Detect(workspace.Root),
            [],
            OwnedIds,
            _ => new Version(3, 4, 0));

        File.ReadAllBytes(file).Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        workspace.ReadFile("Directory.Packages.props").Should().Contain("Version=\"3.4.0\"");
    }

    [Fact]
    public void HandleFile_DoesNotWrite_InDryRun_ButReportsTheSameOutcome()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        using var logger = testOutput.BuildLogger();

        var actual = DirectoryPackagesPropsHelper.HandleFile(
            logger,
            CentralPackageManagementHelper.Detect(workspace.Root),
            [],
            OwnedIds,
            _ => new Version(3, 4, 0),
            dryRun: true);

        actual.Outcome.Should().Be(FileUpdateOutcome.Updated);
        actual.PackageBumps.Should().ContainSingle();
        workspace.ReadFile("Directory.Packages.props").Should().Be(PackagesProps);
    }

    [Fact]
    public void HandleFile_DoesNotRewriteFile_WhenNothingChanges()
    {
        using var workspace = new TestWorkspace();
        var file = workspace.WriteFile("Directory.Packages.props", PackagesProps);
        var knownWriteTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(file.FullName, knownWriteTime);
        using var logger = testOutput.BuildLogger();

        var actual = DirectoryPackagesPropsHelper.HandleFile(
            logger,
            CentralPackageManagementHelper.Detect(workspace.Root),
            [],
            OwnedIds,
            _ => new Version(3, 1, 1));

        actual.Outcome.Should().Be(FileUpdateOutcome.Unchanged);
        File.GetLastWriteTimeUtc(file.FullName).Should().Be(knownWriteTime);
    }

    [Fact]
    public void HandleFile_Skips_WhenThereIsNoPackagesPropsFile()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Build.props", "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");
        using var logger = testOutput.BuildLogger();

        var actual = DirectoryPackagesPropsHelper.HandleFile(
            logger,
            CentralPackageManagementHelper.Detect(workspace.Root),
            [],
            OwnedIds,
            _ => new Version(3, 4, 0));

        actual.Outcome.Should().Be(FileUpdateOutcome.Skipped);
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
    }
}
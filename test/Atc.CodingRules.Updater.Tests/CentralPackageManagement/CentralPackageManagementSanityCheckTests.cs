namespace Atc.CodingRules.Updater.Tests.CentralPackageManagement;

/// <summary>
/// Sanity checks that stop a run before it writes anything into a repository whose Central
/// Package Management (CPM) state would fail restore.
/// </summary>
public sealed class CentralPackageManagementSanityCheckTests
{
    private const string PackagesProps = """
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

    private const string VersionlessProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="Atc" />
          </ItemGroup>
        </Project>
        """;

    [Fact]
    public void CheckFilesAndCollect_ReportsNothing_WhenCpmRepositoryIsConsistent()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile("src/App/App.csproj", VersionlessProject);

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().BeEmpty();
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsError_WhenPackageReferenceHasVersionUnderCpm()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        var project = workspace.WriteFile(
            "src/App/App.csproj",
            "<Project><ItemGroup><PackageReference Include=\"Atc\" Version=\"3.0.186\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().ContainSingle(x => x.Code == "CpmVersionOnPackageReference")
            .Which.Should().Match<SanityCheckDiagnostic>(x =>
                x.Severity == SanityCheckSeverity.Error &&
                x.FilePath == project.FullName);
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsError_WhenDirectoryBuildPropsHasVersionedReferenceUnderCpm()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile(
            "test/Directory.Build.props",
            "<Project><ItemGroup><PackageReference Include=\"Atc\" Version=\"3.0.186\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().ContainSingle(x => x.Code == "CpmVersionOnPackageReference");
    }

    [Fact]
    public void CheckFilesAndCollect_IgnoresGeneratedProjectsUnderObj()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile(
            "src/Functions/obj/Debug/net10.0/WorkerExtensions/WorkerExtensions.csproj",
            "<Project><ItemGroup><PackageReference Include=\"Microsoft.Azure.WebJobs\" Version=\"3.0.41\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().BeEmpty();
    }

    [Fact]
    public void CheckFilesAndCollect_IgnoresImplicitlyDefinedVersionedReference()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile(
            "src/App/App.csproj",
            "<Project><ItemGroup><PackageReference Include=\"Microsoft.NETCore.App\" Version=\"9.0.0\" IsImplicitlyDefined=\"true\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().BeEmpty();
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsError_WhenVersionlessReferenceHasNoPackageVersion()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        var project = workspace.WriteFile(
            "src/App/App.csproj",
            "<Project><ItemGroup><PackageReference Include=\"Unknown.Package\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().ContainSingle(x => x.Code == "CpmMissingPackageVersion")
            .Which.Should().Match<SanityCheckDiagnostic>(x =>
                x.Severity == SanityCheckSeverity.Error &&
                x.FilePath == project.FullName);
    }

    [Fact]
    public void CheckFilesAndCollect_DoesNotReportMissingPackageVersion_WhenReferenceUsesVersionOverride()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile(
            "src/App/App.csproj",
            "<Project><ItemGroup><PackageReference Include=\"Unknown.Package\" VersionOverride=\"1.0.0\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().NotContain(x => x.Code == "CpmMissingPackageVersion");
    }

    [Fact]
    public void CheckFilesAndCollect_DoesNotReportMissingPackageVersion_WhenPackagesPropsImportsAnotherFile()
    {
        const string packagesPropsWithImport = """
            <Project>
              <Import Project="eng/Versions.props" />
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
            </Project>
            """;

        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", packagesPropsWithImport);
        workspace.WriteFile(
            "src/App/App.csproj",
            "<Project><ItemGroup><PackageReference Include=\"Defined.Elsewhere\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().NotContain(x => x.Code == "CpmMissingPackageVersion");
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsWarning_WhenGlobalPackageReferenceIsAlsoReferenced()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile(
            "src/App/App.csproj",
            "<Project><ItemGroup><PackageReference Include=\"AsyncFixer\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().ContainSingle(x => x.Code == "CpmDuplicateGlobalAndPackageReference")
            .Which.Severity.Should().Be(SanityCheckSeverity.Warning);
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsWarning_WhenProjectOptsOutOfCpm()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile(
            "src/Legacy/Legacy.csproj",
            "<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().ContainSingle(x => x.Code == "CpmProjectOptOut")
            .Which.Severity.Should().Be(SanityCheckSeverity.Warning);
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsNoError_ForVersionedReferencesInAProjectThatOptsOut()
    {
        // An opted-out project legitimately keeps versions on its references; a run that used to
        // work must not be stopped by it.
        const string optedOutProject = """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Legacy.Package" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """;

        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile("src/Legacy/Legacy.csproj", optedOutProject);

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().NotContain(x => x.Severity == SanityCheckSeverity.Error);
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsWarningNotError_ForConditionalVersionedReference()
    {
        // The dual-mode pattern restores fine; the condition cannot be evaluated here, so an
        // error would block a working repository.
        const string dualModeProps = """
            <Project>
              <ItemGroup Condition="'$(ManagePackageVersionsCentrally)' != 'true'">
                <PackageReference Include="Atc" Version="3.0.186" />
              </ItemGroup>
            </Project>
            """;

        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile("src/Directory.Build.props", dualModeProps);

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().ContainSingle(x => x.Code == "CpmVersionOnPackageReference")
            .Which.Severity.Should().Be(SanityCheckSeverity.Warning);
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsWarningNotError_ForConditionalVersionlessReferenceWithoutPackageVersion()
    {
        const string conditionalProject = """
            <Project>
              <ItemGroup Condition="'$(TargetFramework)' == 'net48'">
                <PackageReference Include="Only.On.Net48" />
              </ItemGroup>
            </Project>
            """;

        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile("src/App/App.csproj", conditionalProject);

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().ContainSingle(x => x.Code == "CpmMissingPackageVersion")
            .Which.Severity.Should().Be(SanityCheckSeverity.Warning);
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsWarning_WhenPackagesPropsExistsWithoutFlag()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", "<Project />");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().ContainSingle(x => x.Code == "CpmPackagesPropsWithoutFlag")
            .Which.Severity.Should().Be(SanityCheckSeverity.Warning);
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsNoCpmDiagnostics_WhenRepositoryDoesNotUseCpm()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile(
            "src/App/App.csproj",
            "<Project><ItemGroup><PackageReference Include=\"Atc\" Version=\"3.0.186\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(workspace.Root, SupportedProjectTargetType.DotNet10);

        actual.Should().BeEmpty();
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsNoCpmDiagnostics_WhenModeIsDisabled()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Packages.props", PackagesProps);
        workspace.WriteFile(
            "src/App/App.csproj",
            "<Project><ItemGroup><PackageReference Include=\"Atc\" Version=\"3.0.186\" /></ItemGroup></Project>");

        var actual = ProjectSanityCheckHelper.CheckFilesAndCollect(
            workspace.Root,
            SupportedProjectTargetType.DotNet10,
            CentralPackageManagementMode.Disabled);

        actual.Should().BeEmpty();
    }
}
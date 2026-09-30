namespace Atc.CodingRules.Updater.Tests.CentralPackageManagement.Migration;

/// <summary>
/// Plans the conversion of a non-CPM repository to Central Package Management. The planner
/// only reads; every change it would make is returned as new file content.
/// </summary>
public sealed class CpmMigrationPlannerTests
{
    private const string PackagesPropsFileName = "Directory.Packages.props";

    [Fact]
    public void Plan_ReportsAlreadyMigrated_WhenCpmIsOn()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile(PackagesPropsFileName, "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.AlreadyMigrated.Should().BeTrue();
        actual.CanMigrate.Should().BeFalse();
        actual.FileChanges.Should().BeEmpty();
    }

    [Fact]
    public void Plan_Blocks_WhenPackagesPropsExistsWithoutFlag()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile(PackagesPropsFileName, "<Project />");
        workspace.WriteFile("src/App/App.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.186\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.Blockers.Should().ContainSingle(x => x.Code == "PackagesPropsWithoutFlag");
        actual.FileChanges.Should().BeEmpty();
    }

    [Fact]
    public void Plan_MovesVersionsIntoNewPackagesProps()
    {
        using var workspace = new TestWorkspace();
        var project = workspace.WriteFile("src/App/App.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.186\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.CanMigrate.Should().BeTrue();
        actual.FileChanges[project.FullName].Should().Contain("<PackageReference Include=\"Atc\" />");

        var packagesProps = actual.FileChanges[Path.Combine(workspace.Root.FullName, PackagesPropsFileName)];
        packagesProps.Should().Contain("<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>");
        PackageItemParser.Parse(packagesProps)!
            .Should().ContainSingle(x => x.Id == "Atc" && x.Version == "3.0.186" && x.ItemType == PackageItemType.PackageVersion);
    }

    [Fact]
    public void Plan_DoesNotTurnOnTransitivePinning()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.186\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.FileChanges[Path.Combine(workspace.Root.FullName, PackagesPropsFileName)]
            .Should().NotContain("CentralPackageTransitivePinningEnabled");
    }

    [Fact]
    public void Plan_KeepsDivergentVersionsWithVersionOverride_ByDefault()
    {
        using var workspace = new TestWorkspace();
        var newer = workspace.WriteFile("src/A/A.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.186\" />"));
        var older = workspace.WriteFile("src/B/B.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.100\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.Packages.Should().ContainSingle(x => x.Id == "Atc").Which.Version.Should().Be("3.0.186");
        actual.FileChanges[newer.FullName].Should().Contain("<PackageReference Include=\"Atc\" />");
        actual.FileChanges[older.FullName].Should().Contain("<PackageReference Include=\"Atc\" VersionOverride=\"3.0.100\" />");
        actual.Overrides.Should().ContainSingle(x => x.Id == "Atc" && x.Version == "3.0.100" && x.FilePath == older.FullName);
    }

    [Fact]
    public void Plan_UnifiesDivergentVersionsToHighest_WhenAskedTo()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/A/A.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.186\" />"));
        var older = workspace.WriteFile("src/B/B.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.100\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root, unifyVersions: true);

        actual.Overrides.Should().BeEmpty();
        actual.FileChanges[older.FullName].Should().Contain("<PackageReference Include=\"Atc\" />");
    }

    [Fact]
    public void Plan_PicksReleaseOverPrereleaseOfSameVersion_AsHighest()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/A/A.csproj", Project("<PackageReference Include=\"Pkg\" Version=\"2.0.0-beta.1\" />"));
        workspace.WriteFile("src/B/B.csproj", Project("<PackageReference Include=\"Pkg\" Version=\"2.0.0\" />"));
        workspace.WriteFile("src/C/C.csproj", Project("<PackageReference Include=\"Pkg\" Version=\"1.9.0\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.Packages.Should().ContainSingle(x => x.Id == "Pkg").Which.Version.Should().Be("2.0.0");
    }

    [Fact]
    public void Plan_TurnsRootCodeAnalyzersIntoGlobalPackageReferences()
    {
        using var workspace = new TestWorkspace();
        var rootProps = workspace.WriteFile("Directory.Build.props", TestFixtures.DistributionRootProps());

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.Packages.Where(x => x.ItemType == PackageItemType.GlobalPackageReference)
            .Should().HaveCount(5);
        PackageItemParser.Parse(actual.FileChanges[rootProps.FullName])!.Should().BeEmpty();
        actual.FileChanges[rootProps.FullName].Should().Contain("Directory.Packages.props");
    }

    [Fact]
    public void Plan_KeepsAnalyzersAsVersionlessReferences_WhenSdkDoesNotSupportGlobalPackageReference()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("global.json", """{ "sdk": { "version": "6.0.400" } }""");
        var rootProps = workspace.WriteFile("Directory.Build.props", TestFixtures.DistributionRootProps());

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.Packages.Should().HaveCount(5).And.OnlyContain(x => x.ItemType == PackageItemType.PackageVersion);
        PackageItemParser.Parse(actual.FileChanges[rootProps.FullName])!
            .Should().HaveCount(5).And.OnlyContain(x => x.Version == null);
    }

    [Fact]
    public void Plan_KeepsReferenceMetadata()
    {
        using var workspace = new TestWorkspace();
        var testProps = workspace.WriteFile("test/Directory.Build.props", TestFixtures.DistributionTestProps());

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.FileChanges[testProps.FullName].Should().Contain("<PrivateAssets>all</PrivateAssets>");
        PackageItemParser.Parse(actual.FileChanges[testProps.FullName])!.Should().OnlyContain(x => x.Version == null);
    }

    [Fact]
    public void Plan_MigratesPropertyVersionAsIs()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", Project("<PackageReference Include=\"Atc\" Version=\"$(AtcVersion)\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.Packages.Should().ContainSingle(x => x.Id == "Atc").Which.Version.Should().Be("$(AtcVersion)");
    }

    [Fact]
    public void Plan_LeavesImplicitlyDefinedReferencesAlone()
    {
        using var workspace = new TestWorkspace();
        var project = workspace.WriteFile(
            "src/App/App.csproj",
            Project("<PackageReference Include=\"Microsoft.NETCore.App\" Version=\"9.0.0\" IsImplicitlyDefined=\"true\" /><PackageReference Include=\"Atc\" Version=\"3.0.186\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.Packages.Should().NotContain(x => x.Id == "Microsoft.NETCore.App");
        actual.FileChanges[project.FullName].Should().Contain("Include=\"Microsoft.NETCore.App\" Version=\"9.0.0\"");
    }

    [Fact]
    public void Plan_AllowsConditionalReferences_WhenEveryVariantHasTheSameVersion()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile(
            "src/App/App.csproj",
            Project("<PackageReference Include=\"Atc\" Version=\"3.0.186\" Condition=\"'$(TargetFramework)' == 'net10.0'\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.CanMigrate.Should().BeTrue();
    }

    [Fact]
    public void Plan_IgnoresBuildOutput()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/obj/Debug/Generated/Generated.csproj", Project("<PackageReference Include=\"Atc\" Version=\"1.*\" />"));
        workspace.WriteFile("src/App/App.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.186\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.CanMigrate.Should().BeTrue();
        actual.FileChanges.Keys.Should().NotContain(x => x.Contains("obj", StringComparison.Ordinal));
    }

    [Fact]
    public void Plan_ReportsEveryBlockerInOnePass_AndPlansNoChanges()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/Floating/Floating.csproj", Project("<PackageReference Include=\"A\" Version=\"1.*\" />"));
        workspace.WriteFile("src/Update/Update.csproj", Project("<PackageReference Update=\"B\" Version=\"1.0.0\" />"));
        workspace.WriteFile("src/Override/Override.csproj", Project("<PackageReference Include=\"C\" VersionOverride=\"1.0.0\" />"));
        workspace.WriteFile("src/NoVersion/NoVersion.csproj", Project("<PackageReference Include=\"D\" />"));
        workspace.WriteFile("src/Legacy/packages.config", "<packages />");
        workspace.WriteFile(
            "src/Conditional/Conditional.csproj",
            Project("<PackageReference Include=\"E\" Version=\"1.0.0\" Condition=\"'$(X)' == 'a'\" /><PackageReference Include=\"E\" Version=\"2.0.0\" Condition=\"'$(X)' == 'b'\" />"));
        workspace.WriteFile("eng/Packages.props", Project("<PackageReference Include=\"F\" Version=\"1.0.0\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        actual.Blockers.Select(x => x.Code).Should().BeEquivalentTo(
            "FloatingVersion",
            "PackageReferenceUpdate",
            "VersionOverrideExists",
            "MissingVersion",
            "PackagesConfig",
            "ConditionalVersionsDiffer",
            "PackageReferenceInOtherImportedFile");
        actual.CanMigrate.Should().BeFalse();
        actual.FileChanges.Should().BeEmpty();
    }

    [Fact]
    public void Plan_ProducesAConsistentCpmLayout()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("Directory.Build.props", TestFixtures.DistributionRootProps());
        workspace.WriteFile("test/Directory.Build.props", TestFixtures.DistributionTestProps());
        workspace.WriteFile("src/A/A.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.186\" />"));
        workspace.WriteFile("src/B/B.csproj", Project("<PackageReference Include=\"Atc\" Version=\"3.0.100\" />"));

        var actual = CpmMigrationPlanner.Plan(workspace.Root);

        var packagesProps = PackageItemParser.Parse(actual.FileChanges[Path.Combine(workspace.Root.FullName, PackagesPropsFileName)])!;
        var declared = packagesProps.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, content) in actual.FileChanges.Where(x => !x.Key.EndsWith(PackagesPropsFileName, StringComparison.Ordinal)))
        {
            foreach (var reference in PackageItemParser.Parse(content)!.Where(x => x.ItemType == PackageItemType.PackageReference))
            {
                reference.Version.Should().BeNull();
                declared.Should().Contain(reference.Id);
            }
        }
    }

    private static string Project(string references)
        => $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    {references}\n  </ItemGroup>\n</Project>";
}
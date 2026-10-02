namespace Atc.CodingRules.Updater.Tests.Commands;

/// <summary>
/// <c>cpm validate</c> shows the migration plan and every blocker. It never writes, never runs
/// git or restore, and its exit code says whether <c>cpm migrate</c> could run.
/// </summary>
public sealed class CpmValidateCommandTests
{
    private const string ProjectContent = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Atc\" Version=\"3.0.186\" />\n  </ItemGroup>\n</Project>";

    [Fact]
    public async Task ExecuteInternalAsync_ReturnsSuccess_AndWritesNothing_WhenMigratable()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent);

        var actual = await Validate(workspace);

        actual.Should().Be(ConsoleExitStatusCodes.Success);
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
        workspace.ReadFile("src/App/App.csproj").Should().Be(ProjectContent);
    }

    [Fact]
    public async Task ExecuteInternalAsync_ReturnsFailure_WhenBlocked()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent.Replace("3.0.186", "3.*", StringComparison.Ordinal));

        var actual = await Validate(workspace);

        actual.Should().Be(ConsoleExitStatusCodes.Failure);
    }

    [Fact]
    public async Task ExecuteInternalAsync_ReturnsSuccess_WhenAlreadyMigrated()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile(
            "Directory.Packages.props",
            "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");

        var actual = await Validate(workspace);

        actual.Should().Be(ConsoleExitStatusCodes.Success);
    }

    private static Task<int> Validate(TestWorkspace workspace)
        => new CpmValidateCommand(NullLogger<CpmValidateCommand>.Instance).ExecuteInternalAsync(
            new CpmCommandSettings { ProjectPath = workspace.Root.FullName },
            new ThrowingCpmMigrationEnvironment(),
            TestContext.Current.CancellationToken);
}
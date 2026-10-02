namespace Atc.CodingRules.Updater.Tests.Commands;

public sealed class CpmMigrateCommandTests
{
    private const string ProjectContent = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Atc\" Version=\"3.0.186\" />\n  </ItemGroup>\n</Project>";

    [Fact]
    public async Task ExecuteInternalAsync_DryRun_ReturnsSuccess_AndWritesNothing()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent);
        var command = new CpmMigrateCommand(NullLogger<CpmMigrateCommand>.Instance);

        var actual = await command.ExecuteInternalAsync(
            new CpmMigrateCommandSettings { ProjectPath = workspace.Root.FullName, DryRun = true },
            new ThrowingCpmMigrationEnvironment(),
            TestContext.Current.CancellationToken);

        actual.Should().Be(ConsoleExitStatusCodes.Success);
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
        workspace.ReadFile("src/App/App.csproj").Should().Be(ProjectContent);
    }

    [Fact]
    public async Task ExecuteInternalAsync_ReturnsFailure_WhenBlocked()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", ProjectContent.Replace("3.0.186", "3.*", StringComparison.Ordinal));
        var command = new CpmMigrateCommand(NullLogger<CpmMigrateCommand>.Instance);

        var actual = await command.ExecuteInternalAsync(
            new CpmMigrateCommandSettings { ProjectPath = workspace.Root.FullName },
            new ThrowingCpmMigrationEnvironment(),
            TestContext.Current.CancellationToken);

        actual.Should().Be(ConsoleExitStatusCodes.Failure);
    }

    [Fact]
    public void Settings_DoNotOfferTheUnusedProjectTargetOption()
    {
        // The cpm commands never read --project-target, so they must not inherit it.
        typeof(CpmCommandSettings).IsAssignableTo(typeof(ProjectCommandSettings)).Should().BeFalse();
        typeof(CpmMigrateCommandSettings).IsAssignableTo(typeof(CpmCommandSettings)).Should().BeTrue();
    }
}
namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Runs the real CLI as a process, as a user would. Unit tests of the argument rewriting and of
/// the commands cannot catch a command that Spectre never reaches (the former
/// <c>migrate-to-cpm -p .</c> was rewritten into <c>run migrate-to-cpm</c>); these can. None of
/// these commands needs the network.
/// </summary>
public sealed class ProgramProcessTests
{
    private const string MigratableProject = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Atc\" Version=\"3.0.186\" />\n  </ItemGroup>\n</Project>";

    private static readonly string CliAssembly = Path.Combine(AppContext.BaseDirectory, "atc-coding-rules-updater.dll");

    [Fact]
    public async Task CpmHelp_ListsValidateAndMigrate()
    {
        using var workspace = new TestWorkspace();

        var (exitCode, output) = await RunCli(workspace, "cpm", "--help");

        exitCode.Should().Be(0);
        output.Should().Contain("validate").And.Contain("migrate");
    }

    [Fact]
    public async Task CpmValidate_WithDotProjectPath_RunsTheValidateCommand()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile("src/App/App.csproj", MigratableProject);

        var (exitCode, output) = await RunCli(workspace, "cpm", "validate", "-p", ".", "--json");

        output.Should().NotContain("Unknown command");
        exitCode.Should().Be(0);
        output.Should().Contain("\"Outcome\": \"DryRun\"");
        workspace.FileExists("Directory.Packages.props").Should().BeFalse();
    }

    [Fact]
    public async Task SanityCheck_AcceptsTheReleasedCamelCaseOptionNames()
    {
        using var workspace = new TestWorkspace();

        var (exitCode, output) = await RunCli(workspace, "sanity-check", "--projectPath", workspace.Root.FullName, "--projectTarget", "DotNet10", "--json");

        exitCode.Should().Be(0, output);
        output.TrimStart().Should().StartWith("[");
    }

    [Fact]
    public async Task RunHelp_DocumentsOnlyKebabCaseOptionNames()
    {
        using var workspace = new TestWorkspace();

        var (exitCode, output) = await RunCli(workspace, "run", "--help");

        exitCode.Should().Be(0);
        output.Should().Contain("--project-path").And.Contain("--options-path");
        output.Should().NotContain("--projectPath").And.NotContain("--optionsPath");
    }

    private static Task<(int ExitCode, string Output)> RunCli(
        TestWorkspace workspace,
        params string[] arguments)
        => DotnetCli.Run(workspace.Root, [CliAssembly, .. arguments]);
}
namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// Regression test for issue #47. Builds a two-project fixture where the project holding most of
/// the violations depends on the one holding the rest.
/// </summary>
/// <remarks>
/// The ATC rules set <c>TreatWarningsAsErrors</c>, so a violating project fails and MSBuild then
/// refuses to build anything depending on it. Collecting errors therefore saw only the first
/// project; collecting warnings with the promotion disabled sees the whole solution.
/// Runs a real <c>dotnet build</c>, hence the integration category.
/// </remarks>
[Trait(Traits.Category, Traits.Categories.Integration)]
public sealed class SuppressionDiagnosticCollectionTests
{
    /// <summary>ProjA holds 2 CS0219 sites, ProjB holds 5 and references ProjA.</summary>
    private const int ProjectASites = 2;
    private const int TotalSites = 7;

    private readonly ITestOutputHelper testOutput;

    public SuppressionDiagnosticCollectionTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public async Task CollectDiagnosticsForSuppressions_SeesViolationsInProjectsThatDependOnAFailingOne()
    {
        var fixture = CreateFixture(nameof(CollectDiagnosticsForSuppressions_SeesViolationsInProjectsThatDependOnAFailingOne));
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        // What the tool used to do: collect errors from a build that stops at the first failure.
        var errorsPass = await DotnetBuildHelper.BuildAndCollectErrors(
            logger,
            fixture,
            runNumber: 1,
            buildFile: null,
            useNugetRestore: true,
            useConfigurationReleaseMode: true,
            timeoutInSec: 600,
            logPrefix: "     ",
            additionalBuildArguments: "",
            cancellationToken: TestContext.Current.CancellationToken);

        // What it does now.
        var warningsPass = await ProjectHelper.CollectDiagnosticsForSuppressions(
            logger,
            fixture,
            runNumber: 2,
            buildFile: null,
            SuppressionBuildOptions.Default,
            TestContext.Current.CancellationToken);

        var errorsFound = errorsPass.GetValueOrDefault("CS0219");
        var warningsFound = warningsPass.GetValueOrDefault("CS0219");

        // Exact counts, which pins both halves of issue #47 at once:
        //
        //  - the errors pass sees only ProjA, because ProjB depends on it and MSBuild will not
        //    build a project whose dependency failed;
        //  - the warnings pass sees every site exactly once. It was 14 for these 7 sites until
        //    Atc.DotNet 3.0.181 stopped counting MSBuild's duplicate diagnostic lines twice.
        errorsFound.Should().Be(
            ProjectASites,
            "a build that stops at the first failure cannot report the dependent project's violations");

        warningsFound.Should().Be(
            TotalSites,
            "every violation site across both projects should be counted exactly once");
    }

    private static DirectoryInfo CreateFixture(string testName)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "atc-coding-rules-updater-suppression-collect-test",
            testName);

        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        Directory.CreateDirectory(root);
        var projA = Directory.CreateDirectory(Path.Combine(root, "ProjA"));
        var projB = Directory.CreateDirectory(Path.Combine(root, "ProjB"));

        // Without a solution, 'dotnet build <dir>' fails with MSB1011 for two projects and never
        // compiles anything.
        const string solution = """
            <Solution>
              <Project Path="ProjA/ProjA.csproj" />
              <Project Path="ProjB/ProjB.csproj" />
            </Solution>
            """;

        const string directoryBuildProps = """
            <Project>
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                <Nullable>disable</Nullable>
              </PropertyGroup>
            </Project>
            """;

        const string projAProject =
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><AssemblyName>ProjA</AssemblyName></PropertyGroup></Project>";

        const string projBProject =
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><AssemblyName>ProjB</AssemblyName></PropertyGroup>" +
            "<ItemGroup><ProjectReference Include=\"..\\ProjA\\ProjA.csproj\" /></ItemGroup></Project>";

        // Two unused variables here, five in ProjB - the latter invisible until the build is
        // allowed to complete.
        const string classA = "public class ClassA { public void M() { int a = 1; int b = 2; } }";
        const string classB = "public class ClassB { public void M() { int a=1; int b=2; int c=3; int d=4; int e=5; } }";

        File.WriteAllText(Path.Combine(root, "Fixture.slnx"), solution);
        File.WriteAllText(Path.Combine(root, "Directory.Build.props"), directoryBuildProps);
        File.WriteAllText(Path.Combine(projA.FullName, "ProjA.csproj"), projAProject);
        File.WriteAllText(Path.Combine(projA.FullName, "ClassA.cs"), classA);
        File.WriteAllText(Path.Combine(projB.FullName, "ProjB.csproj"), projBProject);
        File.WriteAllText(Path.Combine(projB.FullName, "ClassB.cs"), classB);

        return new DirectoryInfo(root);
    }
}
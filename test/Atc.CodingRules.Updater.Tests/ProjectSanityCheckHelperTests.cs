namespace Atc.CodingRules.Updater.Tests;

public sealed class ProjectSanityCheckHelperTests
{
    private static readonly string WorkingDirectory = Path.Combine(
        Path.GetTempPath(),
        "atc-coding-rules-updater-sanity-check-test");

    private readonly ITestOutputHelper testOutput;

    public ProjectSanityCheckHelperTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public void CheckFiles_DoesNotThrow_OnCleanProject()
    {
        var directory = PrepareSubDirectory(nameof(CheckFiles_DoesNotThrow_OnCleanProject));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName>Acme</OrganizationName>",
            "    <RepositoryName>my-repo</RepositoryName>",
            "  </PropertyGroup>",
            "</Project>");

        using var logger = testOutput.BuildLogger();

        // Act + Assert (must not throw)
        ProjectSanityCheckHelper.CheckFiles(
            throwIf: true,
            logger,
            directory,
            SupportedProjectTargetType.DotNet10);

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void CheckFiles_LogsWarning_WhenOrganizationNamePlaceholderRemains()
    {
        var directory = PrepareSubDirectory(nameof(CheckFiles_LogsWarning_WhenOrganizationNamePlaceholderRemains));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName><!-- insert organization name here --></OrganizationName>",
            "    <RepositoryName>my-repo</RepositoryName>",
            "  </PropertyGroup>",
            "</Project>");

        using var logger = testOutput.BuildLogger();

        ProjectSanityCheckHelper.CheckFiles(
            throwIf: false,
            logger,
            directory,
            SupportedProjectTargetType.DotNet10);

        logger.Entries
            .Should().Contain(x => x.Message.Contains("OrganizationName", StringComparison.Ordinal)
                                    && x.Message.Contains("not set yet", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckFiles_LogsWarning_WhenRepositoryNamePlaceholderRemains()
    {
        var directory = PrepareSubDirectory(nameof(CheckFiles_LogsWarning_WhenRepositoryNamePlaceholderRemains));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName>Acme</OrganizationName>",
            "    <RepositoryName><!-- insert repository name here --></RepositoryName>",
            "  </PropertyGroup>",
            "</Project>");

        using var logger = testOutput.BuildLogger();

        ProjectSanityCheckHelper.CheckFiles(
            throwIf: false,
            logger,
            directory,
            SupportedProjectTargetType.DotNet10);

        logger.Entries
            .Should().Contain(x => x.Message.Contains("RepositoryName", StringComparison.Ordinal)
                                    && x.Message.Contains("not set yet", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckFiles_Throws_OnDotNet5_WhenCsProjEnablesNetAnalyzers()
    {
        var directory = PrepareSubDirectory(nameof(CheckFiles_Throws_OnDotNet5_WhenCsProjEnablesNetAnalyzers));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName>Acme</OrganizationName>",
            "    <RepositoryName>my-repo</RepositoryName>",
            "  </PropertyGroup>",
            "</Project>");

        var srcDir = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        File.WriteAllText(
            Path.Combine(srcDir.FullName, "Sample.csproj"),
            string.Join(
                Environment.NewLine,
                "<Project Sdk=\"Microsoft.NET.Sdk\">",
                "  <PropertyGroup>",
                "    <TargetFramework>net5.0</TargetFramework>",
                "    <EnableNETAnalyzers>true</EnableNETAnalyzers>",
                "  </PropertyGroup>",
                "</Project>"));

        using var logger = testOutput.BuildLogger();

        Action act = () => ProjectSanityCheckHelper.CheckFiles(
            throwIf: true,
            logger,
            directory,
            SupportedProjectTargetType.DotNet5);

        act.Should().Throw<DataException>()
            .WithMessage("*EnableNETAnalyzers*");
    }

    [Fact]
    public void CheckFiles_DoesNotThrow_WhenThrowIfFalse_EvenWithViolations()
    {
        var directory = PrepareSubDirectory(nameof(CheckFiles_DoesNotThrow_WhenThrowIfFalse_EvenWithViolations));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName>Acme</OrganizationName>",
            "    <RepositoryName>my-repo</RepositoryName>",
            "    <ImplicitUsings>enable</ImplicitUsings>",
            "  </PropertyGroup>",
            "</Project>");

        var srcDir = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        File.WriteAllText(
            Path.Combine(srcDir.FullName, "Legacy.csproj"),
            string.Join(
                Environment.NewLine,
                "<Project Sdk=\"Microsoft.NET.Sdk\">",
                "  <PropertyGroup>",
                "    <TargetFramework>netcoreapp3.1</TargetFramework>",
                "  </PropertyGroup>",
                "</Project>"));

        using var logger = testOutput.BuildLogger();

        // Act + Assert (must not throw)
        ProjectSanityCheckHelper.CheckFiles(
            throwIf: false,
            logger,
            directory,
            SupportedProjectTargetType.DotNet10);

        logger.Entries
            .Should().Contain(x => x.Message.Contains("TargetFramework", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckFilesAndCollect_ReturnsEmpty_OnCleanProject()
    {
        var directory = PrepareSubDirectory(nameof(CheckFilesAndCollect_ReturnsEmpty_OnCleanProject));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName>Acme</OrganizationName>",
            "    <RepositoryName>my-repo</RepositoryName>",
            "  </PropertyGroup>",
            "</Project>");

        var result = ProjectSanityCheckHelper.CheckFilesAndCollect(directory, SupportedProjectTargetType.DotNet10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsWarnings_ForUnfilledPlaceholders()
    {
        var directory = PrepareSubDirectory(nameof(CheckFilesAndCollect_ReportsWarnings_ForUnfilledPlaceholders));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName><!-- insert organization name here --></OrganizationName>",
            "    <RepositoryName><!-- insert repository name here --></RepositoryName>",
            "  </PropertyGroup>",
            "</Project>");

        var result = ProjectSanityCheckHelper.CheckFilesAndCollect(directory, SupportedProjectTargetType.DotNet10);

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Severity == SanityCheckSeverity.Warning && d.Code == "MissingOrganizationName");
        result.Should().Contain(d => d.Severity == SanityCheckSeverity.Warning && d.Code == "MissingRepositoryName");
    }

    [Fact]
    public void CheckFilesAndCollect_ReportsErrors_ForEnableNetAnalyzersOnDotNet5()
    {
        var directory = PrepareSubDirectory(nameof(CheckFilesAndCollect_ReportsErrors_ForEnableNetAnalyzersOnDotNet5));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName>Acme</OrganizationName>",
            "    <RepositoryName>my-repo</RepositoryName>",
            "  </PropertyGroup>",
            "</Project>");

        var srcDir = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        File.WriteAllText(
            Path.Combine(srcDir.FullName, "Sample.csproj"),
            string.Join(
                Environment.NewLine,
                "<Project Sdk=\"Microsoft.NET.Sdk\">",
                "  <PropertyGroup>",
                "    <TargetFramework>net5.0</TargetFramework>",
                "    <EnableNETAnalyzers>true</EnableNETAnalyzers>",
                "  </PropertyGroup>",
                "</Project>"));

        var result = ProjectSanityCheckHelper.CheckFilesAndCollect(directory, SupportedProjectTargetType.DotNet5);

        result.Should().Contain(d =>
            d.Severity == SanityCheckSeverity.Error
            && d.Code == "EnableNETAnalyzers"
            && d.FilePath != null
            && d.FilePath.EndsWith("Sample.csproj", StringComparison.OrdinalIgnoreCase));
    }

    private static DirectoryInfo PrepareSubDirectory(string testName)
    {
        var path = Path.Combine(WorkingDirectory, testName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        return Directory.CreateDirectory(path);
    }

    private static void WritePropsFile(
        DirectoryInfo directory,
        params string[] lines)
        => File.WriteAllText(
            Path.Combine(directory.FullName, DirectoryBuildPropsHelper.FileName),
            string.Join(Environment.NewLine, lines));
}
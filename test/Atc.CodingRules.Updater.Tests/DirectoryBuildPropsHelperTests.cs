namespace Atc.CodingRules.Updater.Tests;

public sealed class DirectoryBuildPropsHelperTests
{
    private static readonly string WorkingDirectory = Path.Combine(
        Path.GetTempPath(),
        "atc-coding-rules-updater-build-props-test");

    private readonly ITestOutputHelper testOutput;

    public DirectoryBuildPropsHelperTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public void HasFileInsertPlaceholderElement_ReturnsFalse_WhenFileMissing()
    {
        var directory = PrepareSubDirectory(nameof(HasFileInsertPlaceholderElement_ReturnsFalse_WhenFileMissing));

        var result = DirectoryBuildPropsHelper.HasFileInsertPlaceholderElement(
            directory,
            elementName: "OrganizationName",
            elementValue: "insert organization name here");

        result.Should().BeFalse();
    }

    [Fact]
    public void HasFileInsertPlaceholderElement_ReturnsTrue_WhenPlaceholderPresent()
    {
        var directory = PrepareSubDirectory(nameof(HasFileInsertPlaceholderElement_ReturnsTrue_WhenPlaceholderPresent));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName><!-- insert organization name here --></OrganizationName>",
            "  </PropertyGroup>",
            "</Project>");

        var result = DirectoryBuildPropsHelper.HasFileInsertPlaceholderElement(
            directory,
            elementName: "OrganizationName",
            elementValue: "insert organization name here");

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFileInsertPlaceholderElement_ReturnsFalse_WhenPlaceholderAlreadyReplaced()
    {
        var directory = PrepareSubDirectory(nameof(HasFileInsertPlaceholderElement_ReturnsFalse_WhenPlaceholderAlreadyReplaced));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName>Acme</OrganizationName>",
            "  </PropertyGroup>",
            "</Project>");

        var result = DirectoryBuildPropsHelper.HasFileInsertPlaceholderElement(
            directory,
            elementName: "OrganizationName",
            elementValue: "insert organization name here");

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateFileInsertPlaceholderElement_ReplacesPlaceholder_WhenPresent()
    {
        var directory = PrepareSubDirectory(nameof(UpdateFileInsertPlaceholderElement_ReplacesPlaceholder_WhenPresent));
        WritePropsFile(
            directory,
            "<Project>",
            "  <PropertyGroup>",
            "    <RepositoryName><!-- insert repository name here --></RepositoryName>",
            "  </PropertyGroup>",
            "</Project>");

        using var logger = testOutput.BuildLogger();

        DirectoryBuildPropsHelper.UpdateFileInsertPlaceholderElement(
            logger,
            directory,
            elementName: "RepositoryName",
            elementValue: "insert repository name here",
            newElementValue: "my-repo");

        var actual = FileHelper.ReadAllText(new FileInfo(Path.Combine(directory.FullName, DirectoryBuildPropsHelper.FileName)));
        actual.Should().Contain("<RepositoryName>my-repo</RepositoryName>");
        actual.Should().NotContain("insert repository name here");
    }

    [Fact]
    public void UpdateFileInsertPlaceholderElement_IsNoOp_WhenFileMissing()
    {
        var directory = PrepareSubDirectory(nameof(UpdateFileInsertPlaceholderElement_IsNoOp_WhenFileMissing));
        using var logger = testOutput.BuildLogger();

        // Act + Assert (must not throw)
        DirectoryBuildPropsHelper.UpdateFileInsertPlaceholderElement(
            logger,
            directory,
            elementName: "RepositoryName",
            elementValue: "insert repository name here",
            newElementValue: "my-repo");

        File.Exists(Path.Combine(directory.FullName, DirectoryBuildPropsHelper.FileName)).Should().BeFalse();
    }

    [Fact]
    public void UpdateFileInsertPlaceholderElement_IsNoOp_WhenPlaceholderAbsent()
    {
        var directory = PrepareSubDirectory(nameof(UpdateFileInsertPlaceholderElement_IsNoOp_WhenPlaceholderAbsent));
        var original = string.Join(
            Environment.NewLine,
            "<Project>",
            "  <PropertyGroup>",
            "    <OrganizationName>Acme</OrganizationName>",
            "  </PropertyGroup>",
            "</Project>");
        File.WriteAllText(Path.Combine(directory.FullName, DirectoryBuildPropsHelper.FileName), original);

        using var logger = testOutput.BuildLogger();

        DirectoryBuildPropsHelper.UpdateFileInsertPlaceholderElement(
            logger,
            directory,
            elementName: "OrganizationName",
            elementValue: "insert organization name here",
            newElementValue: "Beta");

        var actual = File.ReadAllText(Path.Combine(directory.FullName, DirectoryBuildPropsHelper.FileName));
        actual.Should().Be(original);
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
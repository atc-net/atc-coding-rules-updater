using System.Reflection;
using Xunit.Abstractions;

// ReSharper disable ReturnTypeCanBeEnumerable.Local
namespace Atc.CodingRules.Updater.Tests;

public class EditorConfigHelperTests
{
    private static readonly string WorkingDirectory = Path.Combine(Path.GetTempPath(), "atc-coding-rules-updater-editorconfig-test");
    private readonly FileInfo[] testFiles = CollectTestFiles();

    private readonly ITestOutputHelper testOutput;

    public EditorConfigHelperTests(ITestOutputHelper testOutput)
    {
        this.testOutput = testOutput;
    }

    [Fact]
    public void DotNet6_Root_Update1()
    {
        // Arrange
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("test1.txt");
        var expectedContent = GetContentFromTestFile("Result_DotNet6_Root_1a.txt");

        var contentGit = GetContentFromTestFile("Git_DotNet6_Root_1c.txt");
        var contentFile = GetContentFromTestFile("Git_DotNet6_Root_1a.txt");

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile);

        var actual = FileHelper.ReadAllText(outputFile);

        // Assert
        Assert.Equal(expectedContent, actual);

        logger.Entries
            .Should().HaveCount(1)
            .And.Subject.Should().Contain(x => x.Message.Contains("log-description-part files merged"));
    }

    [Fact]
    public void DotNet6_Root_NothingToUpdate1()
    {
        // Arrange
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("test1.txt");

        var contentGit = GetContentFromTestFile("Git_DotNet6_Root_1c.txt");
        var contentFile = GetContentFromTestFile("Git_DotNet6_Root_1b.txt");

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile);

        // Assert
        Assert.False(outputFile.Exists);

        logger.Entries
            .Should().HaveCount(1)
            .And.Subject.Should().Contain(x => x.Message.Contains("log-description-part nothing to update"));
    }

    [Fact]
    public void DotNet6_Root_NothingToUpdate2()
    {
        // Arrange
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("test1.txt");

        var contentGit = GetContentFromTestFile("Git_DotNet6_Root_1c.txt");
        var contentFile = GetContentFromTestFile("Git_DotNet6_Root_1c.txt");

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile);

        // Assert
        Assert.False(outputFile.Exists);

        logger.Entries
            .Should().HaveCount(1)
            .And.Subject.Should().Contain(x => x.Message.Contains("log-description-part nothing to update"));

    }

    [Fact]
    public void DotNet6_Root_Update3_NewKey()
    {
        // Arrange
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("test1.txt");
        var expectedContent = GetContentFromTestFile("Result_DotNet6_Root_1b.txt");

        var contentGit = GetContentFromTestFile("Git_DotNet6_Root_1d.txt");
        var contentFile = GetContentFromTestFile("File_DotNet6_Root_1a.txt");

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile);

        var actual = FileHelper.ReadAllText(outputFile);

        // Assert
        Assert.Equal(expectedContent, actual);

        logger.Entries
            .Should().HaveCount(5)
            .And.Subject.Should().Contain(x => x.Message.Contains("Duplicate key: dotnet_diagnostic.SA1200.severity"))
            .And.Subject.Should().Contain(x => x.Message.Contains("GitHub section (line 0478)"))
            .And.Subject.Should().Contain(x => x.Message.Contains("Custom section (line 0506)"))
            .And.Subject.Should().Contain(x => x.Message.Contains("New key/value - dotnet_diagnostic.SA1201.severity = none            # https://github.com/atc-net/atc-coding-rules/blob/main/documentation/CodeAnalyzersRules/StyleCop/SA1201.md"));
    }

    private static FileInfo[] CollectTestFiles()
    {
        var testAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        var testBasePath = new DirectoryInfo(baseDir.Split(testAssemblyName, StringSplitOptions.RemoveEmptyEntries)[0]);
        var testFilesPath = Path.Combine(testBasePath.FullName, "Atc.CodingRules.Updater.Tests/TestFilesDistribution");
        return Directory
            .GetFiles(testFilesPath)
            .Select(x => new FileInfo(x))
            .ToArray();
    }

    private static FileInfo PrepareOutputFile(string fileName)
    {
        if (!Directory.Exists(WorkingDirectory))
        {
            Directory.CreateDirectory(WorkingDirectory);
        }

        var file = Path.Combine(WorkingDirectory, fileName);
        if (File.Exists(file))
        {
            File.Delete(file);
        }

        return new FileInfo(file);
    }

    private string GetContentFromTestFile(string fileName)
    {
        var file = testFiles.Single(x => x.Name.Equals(fileName, StringComparison.Ordinal));
        return FileHelper.ReadAllText(file);
    }
}
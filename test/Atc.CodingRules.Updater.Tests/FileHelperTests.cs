namespace Atc.CodingRules.Updater.Tests;

public sealed class FileHelperTests
{
    private static readonly string WorkingDirectory = Path.Combine(
        Path.GetTempPath(),
        "atc-coding-rules-updater-file-helper-test");

    [Fact]
    public void ContainsSolutionOrProjectFile_ReturnsTrue_WhenOnlySlnxSolutionExists()
    {
        var directory = PrepareSubDirectory(nameof(ContainsSolutionOrProjectFile_ReturnsTrue_WhenOnlySlnxSolutionExists));
        WriteFile(directory, "MyProject.slnx", "<Solution />");

        var actual = FileHelper.ContainsSolutionOrProjectFile(directory);

        actual.Should().BeTrue();
    }

    [Fact]
    public void IsSolutionOrProjectFile_ReturnsTrue_ForSlnxSolutionFile()
    {
        var directory = PrepareSubDirectory(nameof(IsSolutionOrProjectFile_ReturnsTrue_ForSlnxSolutionFile));
        var file = WriteFile(directory, "MyProject.slnx", "<Solution />");

        var actual = FileHelper.IsSolutionOrProjectFile(file);

        actual.Should().BeTrue();
    }

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData("node_modules")]
    public void SearchAllForElement_IgnoresGeneratedAndVendorDirectories(
        string excludedDirectory)
    {
        var directory = PrepareSubDirectory(
            $"{nameof(SearchAllForElement_IgnoresGeneratedAndVendorDirectories)}-{excludedDirectory}");

        var nested = Directory.CreateDirectory(
            Path.Combine(directory.FullName, excludedDirectory, "Debug", "net10.0"));

        File.WriteAllText(
            Path.Combine(nested.FullName, "Generated.csproj"),
            "<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var actual = FileHelper.SearchAllForElement(
            directory,
            "*.csproj",
            "TargetFramework",
            "net10.0");

        actual.Should().BeEmpty();
    }

    [Fact]
    public void SearchAllForElement_FindsMatchesInRealProjectDirectories()
    {
        var directory = PrepareSubDirectory(nameof(SearchAllForElement_FindsMatchesInRealProjectDirectories));

        var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src", "MyApp"));
        File.WriteAllText(
            Path.Combine(src.FullName, "MyApp.csproj"),
            "<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        var actual = FileHelper.SearchAllForElement(
            directory,
            "*.csproj",
            "TargetFramework",
            "net10.0");

        actual.Should().ContainSingle(x => x.Name.Equals("MyApp.csproj", StringComparison.Ordinal));
    }

    [Fact]
    public void ContainsEditorConfigFile_ReturnsTrue_WhenEditorConfigPresent()
    {
        var directory = PrepareSubDirectory(nameof(ContainsEditorConfigFile_ReturnsTrue_WhenEditorConfigPresent));
        WriteFile(directory, ".editorconfig", "root = true");

        var actual = FileHelper.ContainsEditorConfigFile(directory);

        actual.Should().BeTrue();
    }

    [Fact]
    public void ContainsEditorConfigFile_ReturnsFalse_WhenEditorConfigAbsent()
    {
        var directory = PrepareSubDirectory(nameof(ContainsEditorConfigFile_ReturnsFalse_WhenEditorConfigAbsent));
        WriteFile(directory, "readme.md", "hello");

        var actual = FileHelper.ContainsEditorConfigFile(directory);

        actual.Should().BeFalse();
    }

    [Fact]
    public void AreFilesEqual_ReturnsFalse_WhenContentDiffersButLengthMatches()
    {
        // Same length, no '# Version' / '# Updated' / '# Distribution' headers to disagree on,
        // but the files are genuinely different.
        const string dataA = "root = true\r\nindent_size = 2\r\n";
        const string dataB = "root = true\r\nindent_size = 4\r\n";

        var actual = FileHelper.AreFilesEqual(dataA, dataB);

        actual.Should().BeFalse();
    }

    [Fact]
    public void AreFilesEqual_ReturnsTrue_ForIdenticalContent()
    {
        const string data = "root = true\r\nindent_size = 2\r\n";

        var actual = FileHelper.AreFilesEqual(data, data);

        actual.Should().BeTrue();
    }

    [Fact]
    public void AreFilesEqual_ReturnsTrue_WhenOnlyLineEndingsDiffer()
    {
        const string dataA = "root = true\r\nindent_size = 2";
        const string dataB = "root = true\nindent_size = 2";

        var actual = FileHelper.AreFilesEqual(dataA, dataB);

        actual.Should().BeTrue();
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

    private static FileInfo WriteFile(
        DirectoryInfo directory,
        string fileName,
        string content)
    {
        var path = Path.Combine(directory.FullName, fileName);
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }
}
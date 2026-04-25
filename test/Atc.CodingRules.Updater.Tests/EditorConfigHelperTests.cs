// ReSharper disable ReturnTypeCanBeEnumerable.Local
namespace Atc.CodingRules.Updater.Tests;

public sealed class EditorConfigHelperTests
{
    private static readonly string WorkingDirectory = Path.Combine(Path.GetTempPath(), "atc-coding-rules-updater-editorconfig-test");
    private readonly FileInfo[] testFiles = CollectTestFiles();

    private readonly ITestOutputHelper testOutput;

    public EditorConfigHelperTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

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

    [Fact]
    public void HandleFile_DryRun_DoesNotWriteFile_WhenContentDiffers()
    {
        // Arrange: identical structural shape as Update3_NewKey but with dryRun=true.
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("dry-run-test.editorconfig");

        var contentGit = GetContentFromTestFile("Git_DotNet6_Root_1d.txt");
        var contentFile = GetContentFromTestFile("File_DotNet6_Root_1a.txt");

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile,
            dryRun: true);

        // Assert: file is not created, log states "would merge"
        outputFile.Refresh();
        outputFile.Exists.Should().BeFalse();
        logger.Entries
            .Should().Contain(x => x.Message.Contains("(dry-run)", StringComparison.Ordinal)
                                    && x.Message.Contains("would merge", StringComparison.Ordinal));
    }

    [Fact]
    public void HandleFile_DryRun_DoesNotCreateFile_WhenLocalAbsent()
    {
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("dry-run-create.editorconfig");

        var contentGit = GetContentFromTestFile("Git_DotNet6_Root_1c.txt");
        var contentFile = string.Empty;

        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile,
            dryRun: true);

        outputFile.Refresh();
        outputFile.Exists.Should().BeFalse();
        logger.Entries
            .Should().Contain(x => x.Message.Contains("(dry-run)", StringComparison.Ordinal)
                                    && x.Message.Contains("would create", StringComparison.Ordinal));
    }

    [Fact]
    public void HandleFile_DoesNotOverwriteFile_WhenGitContentIsEmpty()
    {
        // Arrange: an empty contentGit (404 from upstream) used to fall through to the merge
        // path and overwrite the user's file with an empty base section. Now we skip with a warning.
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("empty-git-content.editorconfig");
        var existingContent = string.Join(
            Environment.NewLine,
            "root = true",
            string.Empty,
            "[*.cs]",
            "dotnet_diagnostic.SA1234.severity = warning");
        File.WriteAllText(outputFile.FullName, existingContent);

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit: string.Empty,
            contentFile: existingContent,
            "log-description-part",
            outputFile);

        // Assert: file untouched, warning logged
        var actual = File.ReadAllText(outputFile.FullName);
        actual.Should().Be(existingContent);
        logger.Entries
            .Should().ContainSingle()
            .Which.Message.Should().Contain("skipped — upstream content is empty");
    }

    [Fact]
    public void HandleFile_EscapesMarkupCharacters_InCustomSectionValue()
    {
        // Arrange: contentGit and contentFile share a dotnet_diagnostic key, but the
        // file's custom section value contains brackets that look like Spectre markup.
        // LogSeverityDiffs must escape those before logging or a Spectre-backed logger crashes.
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("markup-escape.editorconfig");

        var contentGit = string.Join(
            Environment.NewLine,
            "root = true",
            string.Empty,
            "[*.cs]",
            "dotnet_diagnostic.SA1234.severity = warning",
            string.Empty);

        var contentFile = string.Join(
            Environment.NewLine,
            "root = true",
            string.Empty,
            "[*.cs]",
            string.Empty,
            EditorConfigHelper.SectionDivider,
            EditorConfigHelper.CustomSectionHeaderPrefix + EditorConfigHelper.CustomSectionHeaderCodeAnalyzersRulesSuffix,
            EditorConfigHelper.SectionDivider,
            "[*.cs]",
            "dotnet_diagnostic.SA1234.severity = none [PersistentState]",
            string.Empty);

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile);

        // Assert: the duplicate-key warning that quotes the file value must contain the
        // Spectre-escaped form '[[PersistentState]]' rather than the raw '[PersistentState]'.
        logger.Entries
            .Should().Contain(x => x.Message.Contains("[[PersistentState]]", StringComparison.Ordinal));
        logger.Entries
            .Should().NotContain(x => x.Message.Contains("[PersistentState]", StringComparison.Ordinal)
                                       && !x.Message.Contains("[[PersistentState]]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateRootFileRemoveCustomAtcAutogeneratedRuleSuppressions_DoesNotThrow_WhenAutogeneratedHeaderIsFirstLine()
    {
        // Arrange
        var directory = new DirectoryInfo(Path.Combine(WorkingDirectory, "first-line-header"));
        if (directory.Exists)
        {
            directory.Delete(recursive: true);
        }

        directory.Create();
        var editorConfig = new FileInfo(Path.Combine(directory.FullName, EditorConfigHelper.FileName));
        var content = string.Join(
            Environment.NewLine,
            EditorConfigHelper.AutogeneratedCustomSectionHeaderPrefix,
            EditorConfigHelper.SectionDivider,
            "[*.cs]",
            "dotnet_diagnostic.CA1234.severity = none");
        var cancellationToken = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(editorConfig.FullName, content, cancellationToken);

        // Act + Assert (must not throw)
        await EditorConfigHelper.UpdateRootFileRemoveCustomAtcAutogeneratedRuleSuppressions(directory);

        var resultContent = await File.ReadAllTextAsync(editorConfig.FullName, cancellationToken);
        resultContent.Should().NotContain(EditorConfigHelper.AutogeneratedCustomSectionHeaderPrefix);
    }

    [Fact]
    public void HandleFile_LogsOverrideSemantics_WhenDuplicateKeyAcrossSections()
    {
        // Arrange: same dotnet_diagnostic key in git base and the user's custom section.
        // Custom section wins by design; the warning must make that explicit so users can clean up.
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("duplicate-key.editorconfig");

        var contentGit = string.Join(
            Environment.NewLine,
            "root = true",
            string.Empty,
            "[*.cs]",
            "dotnet_diagnostic.SA1234.severity = warning");

        var contentFile = string.Join(
            Environment.NewLine,
            "root = true",
            string.Empty,
            "[*.cs]",
            string.Empty,
            EditorConfigHelper.SectionDivider,
            EditorConfigHelper.CustomSectionHeaderPrefix + EditorConfigHelper.CustomSectionHeaderCodeAnalyzersRulesSuffix,
            EditorConfigHelper.SectionDivider,
            "[*.cs]",
            "dotnet_diagnostic.SA1234.severity = none            # local override");

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile);

        // Assert
        logger.Entries
            .Should().Contain(x => x.Message.Contains("Duplicate key: dotnet_diagnostic.SA1234.severity", StringComparison.Ordinal)
                                    && x.Message.Contains("custom section overrides upstream default", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateRootFileSuppressions_AddThenRemove_RestoresOriginalContent()
    {
        // Arrange
        var directory = new DirectoryInfo(Path.Combine(WorkingDirectory, "suppressions-roundtrip"));
        if (directory.Exists)
        {
            directory.Delete(recursive: true);
        }

        directory.Create();
        var editorConfig = new FileInfo(Path.Combine(directory.FullName, EditorConfigHelper.FileName));
        var originalContent = string.Join(
            Environment.NewLine,
            "root = true",
            string.Empty,
            "[*.cs]",
            "dotnet_diagnostic.SA1234.severity = warning",
            string.Empty,
            EditorConfigHelper.SectionDivider,
            EditorConfigHelper.CustomSectionHeaderPrefix + EditorConfigHelper.CustomSectionHeaderCodeAnalyzersRulesSuffix,
            EditorConfigHelper.SectionDivider,
            "[*.cs]",
            "dotnet_diagnostic.SA1204.severity = none");
        var cancellationToken = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(editorConfig.FullName, originalContent, cancellationToken);

        var suppressions = new List<Tuple<string, List<string>>>
        {
            Tuple.Create(
                "Microsoft.CodeAnalysis.NetAnalyzers",
                new List<string> { "dotnet_diagnostic.CA1707.severity = none\t\t# 1 occurrence" }),
        };

        // Act: add then remove
        await EditorConfigHelper.UpdateRootFileAddCustomAtcAutogeneratedRuleSuppressions(directory, suppressions);
        var afterAdd = await File.ReadAllTextAsync(editorConfig.FullName, cancellationToken);
        afterAdd.Should().Contain(EditorConfigHelper.AutogeneratedCustomSectionHeaderPrefix);
        afterAdd.Should().Contain("dotnet_diagnostic.CA1707.severity = none");

        await EditorConfigHelper.UpdateRootFileRemoveCustomAtcAutogeneratedRuleSuppressions(directory);
        var afterRemove = await File.ReadAllTextAsync(editorConfig.FullName, cancellationToken);

        // Assert: autogen section gone; the user's pre-existing content is preserved.
        afterRemove.Should().NotContain(EditorConfigHelper.AutogeneratedCustomSectionHeaderPrefix);
        afterRemove.Should().NotContain("dotnet_diagnostic.CA1707.severity = none");
        afterRemove.Should().Contain("dotnet_diagnostic.SA1234.severity = warning");
        afterRemove.Should().Contain("dotnet_diagnostic.SA1204.severity = none");
    }

    [Fact]
    public void HandleFile_PreservesCustomSectionOrder_AcrossUpdates()
    {
        // Arrange: file already has two custom sections in a specific order. After a merge the
        // user's section order must survive even when the git content adds a *new* custom section.
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("custom-order.editorconfig");

        var existingFile = string.Join(
            Environment.NewLine,
            "root = true",
            string.Empty,
            "[*.cs]",
            string.Empty,
            EditorConfigHelper.SectionDivider,
            EditorConfigHelper.CustomSectionHeaderPrefix + "Project Rules",
            EditorConfigHelper.SectionDivider,
            "dotnet_diagnostic.SA9001.severity = none",
            string.Empty,
            EditorConfigHelper.SectionDivider,
            EditorConfigHelper.CustomSectionHeaderPrefix + EditorConfigHelper.CustomSectionHeaderCodeAnalyzersRulesSuffix,
            EditorConfigHelper.SectionDivider,
            "dotnet_diagnostic.SA9002.severity = none");

        var newGit = string.Join(
            Environment.NewLine,
            "root = true",
            string.Empty,
            "[*.cs]",
            "dotnet_diagnostic.SA1234.severity = warning");

        // Act
        EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            newGit,
            existingFile,
            "log-description-part",
            outputFile);

        // Assert: both custom sections still present, in original order.
        var actual = FileHelper.ReadAllText(outputFile);
        var idxProject = actual.IndexOf("# Custom - Project Rules", StringComparison.Ordinal);
        var idxCodeAnalyzers = actual.IndexOf(
            "# Custom - " + EditorConfigHelper.CustomSectionHeaderCodeAnalyzersRulesSuffix,
            StringComparison.Ordinal);
        idxProject.Should().BePositive();
        idxCodeAnalyzers.Should().BePositive();
        idxProject.Should().BeLessThan(idxCodeAnalyzers);
        actual.Should().Contain("dotnet_diagnostic.SA9001.severity = none");
        actual.Should().Contain("dotnet_diagnostic.SA9002.severity = none");
    }

    [Fact]
    public void HandleFile_HandlesUtf8BomAndMixedLineEndings()
    {
        // Arrange: the user's file may have a BOM (Encoding.UTF8 with preamble) and mixed CRLF/LF
        // line endings (common when files are edited on multiple platforms). HandleFile must not throw.
        using var logger = testOutput.BuildLogger();
        var outputFile = PrepareOutputFile("malformed.editorconfig");

        var contentGit = string.Join(
            "\n",
            "root = true",
            string.Empty,
            "[*.cs]",
            "dotnet_diagnostic.SA1234.severity = warning");

        // Lead with a UTF-8 BOM character + mix CRLF and LF.
        var contentFile = "﻿root = true\r\n\n[*.cs]\rdotnet_diagnostic.SA1234.severity = warning";

        // Act + Assert: must not throw on parsing or merging.
        Action act = () => EditorConfigHelper.HandleFile(
            logger,
            "log-area",
            contentGit,
            contentFile,
            "log-description-part",
            outputFile);

        act.Should().NotThrow();
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
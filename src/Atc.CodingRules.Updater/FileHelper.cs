namespace Atc.CodingRules.Updater;

/// <summary>
/// Thin wrapper around <c>Atc.Helpers.FileHelper</c> with extras for matching .editorconfig
/// and Directory.Build.props content shape used elsewhere in this project.
/// </summary>
public static class FileHelper
{
    private static readonly string[] SolutionAndProjectExtensions = [".sln", ".slnx", ".csproj"];

    /// <summary>
    /// Directories skipped by <see cref="SearchAllForElement"/>: build output and vendored
    /// dependencies, which can contain generated project files that are not the user's source.
    /// </summary>
    private static readonly string[] ExcludedDirectoryNames = ["bin", "obj", ".git", ".vs", "node_modules"];

    /// <summary>
    /// Newline tokens recognised by line-splitting helpers (CR, LF, CRLF).
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Pass-through to Atc.Helpers shape; consumers expect an array.")]
    public static string[] LineBreaks => Helpers.FileHelper.LineBreaks;

    /// <summary>Reads the whole file as text using the default Atc helper (UTF-8, transparent BOM).</summary>
    public static string ReadAllText(FileInfo file)
        => Helpers.FileHelper.ReadAllText(file);

    /// <summary>Writes <paramref name="content"/> to <paramref name="file"/> asynchronously.</summary>
    public static Task WriteAllTextAsync(
        FileInfo file,
        string content)
        => Helpers.FileHelper.WriteAllTextAsync(file, content);

    /// <summary>
    /// Searches every file under <paramref name="projectPath"/> matching <paramref name="searchPattern"/>
    /// and returns those whose contents contain a matching XML-style element fragment.
    /// </summary>
    /// <param name="projectPath">Root directory to search.</param>
    /// <param name="searchPattern">File-name glob (e.g. <c>"*.csproj"</c>, <c>"Directory.Build.props"</c>).</param>
    /// <param name="elementName">Element local-name to look for (no namespace prefix).</param>
    /// <param name="elementValue">Optional inner-text value; when supplied the match becomes <c>&lt;name&gt;value&lt;/name&gt;</c>.</param>
    /// <param name="searchOption">Recursion mode (defaults to <see cref="SearchOption.AllDirectories"/>).</param>
    /// <param name="stringComparison">String comparer for the contains check (defaults to ordinal).</param>
    public static Collection<FileInfo> SearchAllForElement(
        DirectoryInfo projectPath,
        string searchPattern,
        string elementName,
        string? elementValue = null,
        SearchOption searchOption = SearchOption.AllDirectories,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var result = new Collection<FileInfo>();
        var files = Directory
            .EnumerateFiles(projectPath.FullName, searchPattern, searchOption)
            .Where(x => !IsUnderExcludedDirectory(projectPath.FullName, x));

        foreach (var file in files)
        {
            var fileContent = File.ReadAllText(file);
            var searchText = $"<{elementName}";
            if (elementValue is not null)
            {
                searchText = $"<{elementName}>{elementValue}</{elementName}>";
            }

            if (fileContent.Contains(searchText, stringComparison))
            {
                result.Add(new FileInfo(file));
            }
        }

        return result;
    }

    /// <summary>
    /// Writes <paramref name="fileContent"/> to <paramref name="file"/> and logs a "created" line.
    /// </summary>
    public static void CreateFile(
        ILogger logger,
        FileInfo file,
        string fileContent,
        string descriptionPart)
    {
        ArgumentNullException.ThrowIfNull(file);

        File.WriteAllText(file.FullName, fileContent);
        logger.LogInformation($"{EmojisConstants.FileCreated}   {descriptionPart} created");
    }

    /// <summary>
    /// Returns <c>true</c> when the two strings have identical content, ignoring line-ending style.
    /// </summary>
    /// <remarks>
    /// This used to be a heuristic — equal length plus matching <c># Version</c> / <c># Updated</c> /
    /// <c># Distribution</c> headers — which meant two genuinely different files of the same length
    /// compared equal, and the <c>.editorconfig</c> merge in
    /// <see cref="EditorConfigHelper"/> silently reported "nothing to update".
    /// Newlines are still normalised on both sides so a CRLF/LF difference alone is not a change.
    /// </remarks>
    public static bool AreFilesEqual(
        string dataA,
        string dataB)
    {
        ArgumentNullException.ThrowIfNull(dataA);
        ArgumentNullException.ThrowIfNull(dataB);

        return dataA
            .EnsureEnvironmentNewLines()
            .Equals(dataB.EnsureEnvironmentNewLines(), StringComparison.Ordinal);
    }

    /// <summary>Returns <c>true</c> when <paramref name="directory"/> exists and contains a top-level <c>.editorconfig</c>.</summary>
    public static bool ContainsEditorConfigFile(DirectoryInfo? directory)
        => directory is not null &&
           directory.Exists
           && Directory.GetFiles(directory.FullName)
               .Any(x => Path.GetFileName(x).Equals(EditorConfigHelper.FileName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns <c>true</c> when <paramref name="directory"/> contains at least one <c>.sln</c>, <c>.slnx</c> or <c>.csproj</c>.</summary>
    public static bool ContainsSolutionOrProjectFile(DirectoryInfo? directory)
        => directory is not null &&
           directory.Exists
           && Directory.GetFiles(directory.FullName)
               .Any(x => IsSolutionOrProjectExtension(Path.GetExtension(x)));

    /// <summary>Returns <c>true</c> when <paramref name="file"/> exists and has a <c>.sln</c>, <c>.slnx</c> or <c>.csproj</c> extension.</summary>
    public static bool IsSolutionOrProjectFile(FileInfo? file)
        => file is not null &&
           file.Exists &&
           IsSolutionOrProjectExtension(file.Extension);

    private static bool IsSolutionOrProjectExtension(string extension)
        => SolutionAndProjectExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="filePath"/> sits under a build-output or vendor
    /// directory relative to <paramref name="rootPath"/>. Matching is done on path segments below
    /// the root, so a project that happens to live in a folder called <c>bin</c> is not excluded
    /// wholesale.
    /// </summary>
    private static bool IsUnderExcludedDirectory(
        string rootPath,
        string filePath)
    {
        var relativePath = Path.GetRelativePath(rootPath, filePath);
        var segments = relativePath.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        // The final segment is the file name itself, never a directory.
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (ExcludedDirectoryNames.Contains(segments[i], StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
namespace Atc.CodingRules.Updater;

/// <summary>
/// Thin wrapper around <c>Atc.Helpers.FileHelper</c> with extras for matching .editorconfig
/// and Directory.Build.props content shape used elsewhere in this project.
/// </summary>
public static class FileHelper
{
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
        var files = Directory.GetFiles(projectPath.FullName, searchPattern, searchOption);
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
    /// Coarse equality used by the editor-config / build-props merge logic: returns <c>true</c> when
    /// the two strings have the same length (after normalising newlines) and identical
    /// <c># Version</c>, <c># Updated</c>, and <c># Distribution</c> headers in the first ten lines.
    /// </summary>
    /// <remarks>
    /// This is intentionally a fast pre-check, not byte-equality — it lets the merge logic skip work
    /// when only stale comment metadata might differ. For byte-perfect comparison use
    /// <see cref="string.Equals(string?, StringComparison)"/> directly.
    /// </remarks>
    public static bool AreFilesEqual(
        string dataA,
        string dataB)
    {
        ArgumentNullException.ThrowIfNull(dataA);
        ArgumentNullException.ThrowIfNull(dataB);

        var l1 = dataA.EnsureEnvironmentNewLines().Length;
        var l2 = dataB.EnsureEnvironmentNewLines().Length;

        var isSameFileLength = l1.Equals(l2);
        if (!isSameFileLength)
        {
            return false;
        }

        var headerLinesA = dataA
            .ToLines()
            .Take(10)
            .ToList();

        var headerLinesB = dataB
            .ToLines()
            .Take(10)
            .ToList();

        if (headerLinesA.Find(x => x.StartsWith("# Version", StringComparison.CurrentCultureIgnoreCase)) !=
            headerLinesB.Find(x => x.StartsWith("# Version", StringComparison.CurrentCultureIgnoreCase)))
        {
            return false;
        }

        if (headerLinesA.Find(x => x.StartsWith("# Updated", StringComparison.CurrentCultureIgnoreCase)) !=
            headerLinesB.Find(x => x.StartsWith("# Updated", StringComparison.CurrentCultureIgnoreCase)))
        {
            return false;
        }

        return headerLinesA.Find(x => x.StartsWith("# Distribution", StringComparison.CurrentCultureIgnoreCase)) ==
               headerLinesB.Find(x => x.StartsWith("# Distribution", StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>Returns <c>true</c> when <paramref name="directory"/> exists and contains a top-level <c>.editorconfig</c>.</summary>
    public static bool ContainsEditorConfigFile(DirectoryInfo? directory)
        => directory is not null &&
           directory.Exists
           && Directory.GetFiles(directory.FullName)
               .Any(x => x.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns <c>true</c> when <paramref name="directory"/> contains at least one <c>.sln</c> or <c>.csproj</c>.</summary>
    public static bool ContainsSolutionOrProjectFile(DirectoryInfo? directory)
        => directory is not null &&
           directory.Exists
           && Directory.GetFiles(directory.FullName)
               .Any(x => x.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                         x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns <c>true</c> when <paramref name="file"/> exists and has a <c>.sln</c> or <c>.csproj</c> extension.</summary>
    public static bool IsSolutionOrProjectFile(FileInfo? file)
        => file is not null &&
           file.Exists &&
           (".sln".Equals(file.Extension, StringComparison.OrdinalIgnoreCase) ||
            ".csproj".Equals(file.Extension, StringComparison.OrdinalIgnoreCase));
}
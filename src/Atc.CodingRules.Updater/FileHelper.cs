namespace Atc.CodingRules.Updater;

public static class FileHelper
{
    public static readonly string[] LineBreaks = { "\r\n", "\r", "\n" };

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

            if (fileContent.IndexOf(searchText, stringComparison) != -1)
            {
                result.Add(new FileInfo(file));
            }
        }

        return result;
    }

    public static string ReadAllText(
        FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return file.Exists
            ? File.ReadAllText(file.FullName)
            : string.Empty;
    }

    public static void CreateFile(
        ILogger logger,
        FileInfo file,
        string fileContent,
        string descriptionPart)
    {
        ArgumentNullException.ThrowIfNull(file);

        File.WriteAllText(file.FullName, fileContent);
        logger.LogInformation($"{EmojisConstants.FileCreated}    {descriptionPart} created");
    }

    public static bool IsFileDataLengthEqual(
        string dataA,
        string dataB)
    {
        ArgumentNullException.ThrowIfNull(dataA);
        ArgumentNullException.ThrowIfNull(dataB);

        var l1 = dataA.EnsureEnvironmentNewLines().Length;
        var l2 = dataB.EnsureEnvironmentNewLines().Length;

        return l1.Equals(l2);
    }

    public static bool ContainsEditorConfigFile(DirectoryInfo? directory)
        => directory is not null &&
           directory.Exists
           && Directory.GetFiles(directory.FullName)
               .Any(x => x.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase));

    public static bool ContainsSolutionOrProjectFile(DirectoryInfo? directory)
        => directory is not null &&
           directory.Exists
           && Directory.GetFiles(directory.FullName)
               .Any(x => x.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                         x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

    public static bool IsSolutionOrProjectFile(FileInfo? file)
        => file is not null &&
           file.Exists &&
           (".sln".Equals(file.Extension, StringComparison.OrdinalIgnoreCase) ||
            ".csproj".Equals(file.Extension, StringComparison.OrdinalIgnoreCase));
}
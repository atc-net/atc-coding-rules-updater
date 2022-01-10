namespace Atc.CodingRules.Updater;

public static class FileHelper
{
    public static readonly string[] LineBreaks = { "\r\n", "\r", "\n" };

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
        string rawGitData,
        string descriptionPart)
    {
        File.WriteAllText(file.FullName, rawGitData);
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
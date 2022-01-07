namespace Atc.CodingRules.Updater.CLI;

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

        var l1 = dataA
            .Replace("\r\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Length;

        var l2 = dataB
            .Replace("\r\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Length;

        return l1.Equals(l2);
    }
}
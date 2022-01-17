// ReSharper disable InvertIf
// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI.Models;

public class OptionsMappings
{
    public OptionsFolderMappings Sample { get; set; } = new ();

    public OptionsFolderMappings Src { get; set; } = new ();

    public OptionsFolderMappings Test { get; set; } = new ();

    public bool HasMappingsPaths() =>
        Sample.Paths.Count > 0 ||
        Src.Paths.Count > 0 ||
        Test.Paths.Count > 0;

    public void ResolvePaths(
        DirectoryInfo projectPath)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        if (!HasMappingsPaths())
        {
            return;
        }

        for (var i = 0; i < Sample.Paths.Count; i++)
        {
            if (TryResolvePathIfNeeded(projectPath, Sample.Paths[i], out var newPath))
            {
                Sample.Paths[i] = newPath;
            }
        }

        for (var i = 0; i < Src.Paths.Count; i++)
        {
            if (TryResolvePathIfNeeded(projectPath, Src.Paths[i], out var newPath))
            {
                Src.Paths[i] = newPath;
            }
        }

        for (var i = 0; i < Test.Paths.Count; i++)
        {
            if (TryResolvePathIfNeeded(projectPath, Test.Paths[i], out var newPath))
            {
                Test.Paths[i] = newPath;
            }
        }
    }

    public override string ToString()
        => $"{nameof(Sample)}: ({Sample}), {nameof(Src)}: ({Src}), {nameof(Test)}: ({Test})";

    private static bool TryResolvePathIfNeeded(
        DirectoryInfo projectPath,
        string orgPath,
        out string newPath)
    {
        newPath = string.Empty;
        var di = new DirectoryInfo(orgPath);
        if (di.FullName.Contains("Atc.CodingRules.Updater.CLI", StringComparison.Ordinal))
        {
            if (orgPath.IndexOfAny(new[] { '.', '/', '\\' }) == -1)
            {
                newPath = Path.Combine(projectPath.FullName, orgPath);
                return true;
            }

            if (orgPath.StartsWith("./", StringComparison.Ordinal))
            {
                var s = orgPath.Substring(2).Replace("/", "\\", StringComparison.Ordinal);
                newPath = Path.Combine(projectPath.FullName, s);
                return true;
            }

            if (orgPath.IndexOfAny(new[] { '/', '\\' }) != -1)
            {
                var s = orgPath.Replace("/", "\\", StringComparison.Ordinal);
                newPath = Path.Combine(projectPath.FullName, s);
                return true;
            }
        }

        if (orgPath.StartsWith("\\", StringComparison.Ordinal))
        {
            var s = orgPath.Substring(1).Replace("/", "\\", StringComparison.Ordinal);
            newPath = Path.Combine(projectPath.FullName, s);
            return true;
        }

        return false;
    }
}
// ReSharper disable InvertIf
// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI.Models.Options;

public class OptionsMappings
{
    public OptionsFolderMappings Sample { get; set; } = new();

    public OptionsFolderMappings Src { get; set; } = new();

    public OptionsFolderMappings Test { get; set; } = new();

    public bool HasMappingsPaths() =>
        Sample.Paths.Count > 0 ||
        Src.Paths.Count > 0 ||
        Test.Paths.Count > 0;

    public void ResolvePaths(DirectoryInfo projectPath)
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

        // A fully qualified path - "D:\Code\MyRepo\src", or a UNC share - already says where it
        // points, so it is taken as written. Note this is deliberately not IsPathRooted: on
        // Windows that also accepts the drive-relative "\src", which is one of the forms that
        // does need anchoring.
        if (Path.IsPathFullyQualified(orgPath))
        {
            return false;
        }

        var relativePath = orgPath;

        if (relativePath.StartsWith("./", StringComparison.Ordinal) ||
            relativePath.StartsWith(".\\", StringComparison.Ordinal))
        {
            relativePath = relativePath.Substring(2);
        }

        // A leading separator here spells "from the project root" rather than "from the volume
        // root", since anything genuinely rooted was returned above.
        relativePath = relativePath.TrimStart('/', '\\');

        newPath = Path.Combine(
            projectPath.FullName,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        return true;
    }
}
// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI;

public static class OptionsHelper
{
    public static async Task<OptionsFile> CreateDefault(
        DirectoryInfo projectPath,
        string? settingsOptionsPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var fileInfo = GetOptionsFile(projectPath, settingsOptionsPath);
        if (!fileInfo.Exists)
        {
            return CreateDefaultOptions(projectPath);
        }

        var optionsPath = GetOptionsPath(projectPath, settingsOptionsPath);
        var options = await FileHelper<OptionsFile>.ReadJsonFileToModelAsync(fileInfo, cancellationToken);
        if (options is null)
        {
            return CreateDefaultOptions(projectPath);
        }

        options.Mappings.ResolvePaths(
            optionsPath.EndsWith(".json", StringComparison.CurrentCultureIgnoreCase)
                ? new FileInfo(optionsPath).Directory!
                : new DirectoryInfo(optionsPath));

        return options;
    }

    public static async Task<(bool IsSuccessful, string Error)> CreateOptionsFile(
        DirectoryInfo projectPath,
        ProjectCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(settings);

        var fileInfo = GetOptionsFile(projectPath, settings.GetOptionsPath());
        if (fileInfo.Exists)
        {
            return (false, "File already exist");
        }

        var options = CreateDefaultOptions(projectPath);
        if (settings.ProjectTarget.IsSet)
        {
            options.ProjectTarget = settings.ProjectTarget.Value;
        }

        await FileHelper<OptionsFile>.WriteModelToJsonFileAsync(fileInfo, options, cancellationToken);
        return (true, string.Empty);
    }

    public static async Task<(bool IsSuccessful, string Error)> ValidateOptionsFile(
        DirectoryInfo projectPath,
        string? settingsOptionsPath,
        CancellationToken cancellationToken)
    {
        var fileInfo = GetOptionsFile(projectPath, settingsOptionsPath);
        if (!fileInfo.Exists)
        {
            return (false, "File does not exist");
        }

        var options = await FileHelper<OptionsFile>.ReadJsonFileToModelAsync(fileInfo, cancellationToken);
        return options is null
            ? (false, "File is invalid")
            : (true, string.Empty);
    }

    private static OptionsFile CreateDefaultOptions(DirectoryInfo projectPath)
    {
        var options = new OptionsFile();
        var directories = projectPath.GetDirectories();

        var sampleName = directories.FirstOrDefault(x => x.Name.Equals("sample", StringComparison.OrdinalIgnoreCase))?.Name;
        if (sampleName is not null)
        {
            options.Mappings.Sample.Paths.Add(sampleName);
        }

        var srcName = directories.FirstOrDefault(x => x.Name.Equals("src", StringComparison.OrdinalIgnoreCase))?.Name ?? "src";
        options.Mappings.Src.Paths.Add(srcName);

        var testName = directories.FirstOrDefault(x => x.Name.Equals("test", StringComparison.OrdinalIgnoreCase))?.Name ?? "test";
        options.Mappings.Test.Paths.Add(testName);

        // The names above are bare folder names discovered under projectPath, so they have to be
        // anchored to it here. Without this the no-options-file run resolved them against the
        // current directory instead, and wrote into whatever tree the tool started in.
        options.Mappings.ResolvePaths(projectPath);

        return options;
    }

    private static FileInfo GetOptionsFile(
        DirectoryInfo projectPath,
        string? settingsOptionsPath)
    {
        var optionsPath = GetOptionsPath(projectPath, settingsOptionsPath);

        return optionsPath.EndsWith(".json", StringComparison.Ordinal)
            ? new FileInfo(optionsPath)
            : new FileInfo(Path.Combine(optionsPath, "atc-coding-rules-updater.json"));
    }

    private static string GetOptionsPath(
        DirectoryInfo projectPath,
        string? settingsOptionsPath)
        => string.IsNullOrEmpty(settingsOptionsPath)
            ? projectPath.FullName
            : settingsOptionsPath;
}
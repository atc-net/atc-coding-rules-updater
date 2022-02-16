// ReSharper disable SuggestBaseTypeForParameter

using Atc.CodingRules.Updater.CLI.Models.Options;

namespace Atc.CodingRules.Updater.CLI;

public static class OptionsHelper
{
    public static async Task<OptionsFile> CreateDefault(
        DirectoryInfo projectPath,
        string? settingsOptionsPath)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var fileInfo = GetOptionsFile(projectPath, settingsOptionsPath);
        if (!fileInfo.Exists)
        {
            return CreateDefaultOptions(projectPath);
        }

        var optionsPath = GetOptionsPath(projectPath, settingsOptionsPath);
        var options = await FileHelper<OptionsFile>.ReadJsonFileAndDeserializeAsync(fileInfo);
        if (options is null)
        {
            return CreateDefaultOptions(projectPath);
        }

        options.Mappings.ResolvePaths(new DirectoryInfo(optionsPath));
        return options;
    }

    public static async Task<(bool, string)> CreateOptionsFile(
        DirectoryInfo projectPath,
        ProjectCommandSettings settings)
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

        await FileHelper<OptionsFile>.WriteModelToJsonFileAsync(fileInfo, options);
        return (true, string.Empty);
    }

    public static async Task<(bool isSuccessful, string error)> ValidateOptionsFile(
        DirectoryInfo projectPath,
        string? settingsOptionsPath)
    {
        var fileInfo = GetOptionsFile(projectPath, settingsOptionsPath);
        if (!fileInfo.Exists)
        {
            return (false, "File does not exist");
        }

        var options = await FileHelper<OptionsFile>.ReadJsonFileAndDeserializeAsync(fileInfo);
        return options is null
            ? (false, "File is invalid")
            : (true, string.Empty);
    }

    private static OptionsFile CreateDefaultOptions(
        DirectoryInfo projectPath)
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
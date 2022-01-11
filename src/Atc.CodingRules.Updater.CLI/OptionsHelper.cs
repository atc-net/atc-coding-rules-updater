// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI;

public static class OptionsHelper
{
    public static async Task<Options> CreateDefault(
        DirectoryInfo projectPath,
        string? settingsOptionsPath)
    {
        var fileInfo = GetOptionsFile(projectPath, settingsOptionsPath);
        if (!fileInfo.Exists)
        {
            return CreateDefaultOptions();
        }

        var optionsPath = GetOptionsPath(projectPath, settingsOptionsPath);
        var options = await DeserializeFile(fileInfo);
        if (options is null)
        {
            return CreateDefaultOptions();
        }

        options.Mappings.ResolvePaths(new DirectoryInfo(optionsPath));
        return options;
    }

    public static async Task<(bool, string)> CreateOptionsFile(
        DirectoryInfo projectPath,
        string? settingsOptionsPath)
    {
        var fileInfo = GetOptionsFile(projectPath, settingsOptionsPath);
        if (fileInfo.Exists)
        {
            return (false, "File already exist");
        }

        var options = CreateDefaultOptions();
        var serializeOptions = JsonSerializerOptionsFactory.Create();
        var json = JsonSerializer.Serialize(options, serializeOptions);
        await File.WriteAllTextAsync(fileInfo.FullName, json, Encoding.UTF8);
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

        var options = await DeserializeFile(fileInfo);
        return options is null
            ? (false, "File is invalid")
            : (true, string.Empty);
    }

    private static Options CreateDefaultOptions()
    {
        var options = new Options();
        options.Mappings.Sample.Paths.Add("sample");
        options.Mappings.Src.Paths.Add("src");
        options.Mappings.Test.Paths.Add("test");
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

    private static async Task<Options?> DeserializeFile(
        FileInfo fileInfo)
    {
        var serializeOptions = JsonSerializerOptionsFactory.Create();
        using var stream = new StreamReader(fileInfo.FullName);
        var json = await stream.ReadToEndAsync();
        return JsonSerializer.Deserialize<Options>(json, serializeOptions);
    }
}
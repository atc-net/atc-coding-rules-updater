// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI;

public static class OptionsHelper
{
    public static async Task<OptionRoot> CreateDefault(
        DirectoryInfo rootPath,
        string? settingsOptionsPath)
    {
        var fileInfo = GetOptionsFile(rootPath, settingsOptionsPath);
        if (!fileInfo.Exists)
        {
            return new OptionRoot();
        }

        var optionsPath = GetOptionsPath(rootPath, settingsOptionsPath);
        var options = await DeserializeFile(fileInfo);
        if (options is null)
        {
            return new OptionRoot();
        }

        options.Mappings.ResolvePaths(new DirectoryInfo(optionsPath));
        return options;
    }

    public static async Task<(bool, string)> CreateOptionsFile(
        DirectoryInfo rootPath,
        string? settingsOptionsPath)
    {
        var fileInfo = GetOptionsFile(rootPath, settingsOptionsPath);
        if (fileInfo.Exists)
        {
            return (false, "File already exist");
        }

        var options = new OptionRoot();
        options.Mappings.Sample.Paths.Add(Path.Combine(rootPath.FullName, "sample"));
        options.Mappings.Src.Paths.Add(Path.Combine(rootPath.FullName, "src"));
        options.Mappings.Test.Paths.Add(Path.Combine(rootPath.FullName, "test"));

        var serializeOptions = JsonSerializerOptionsFactory.Create();
        var json = JsonSerializer.Serialize(options, serializeOptions);
        await File.WriteAllTextAsync(fileInfo.FullName, json, Encoding.UTF8);
        return (true, string.Empty);
    }

    public static async Task<(bool isSuccessful, string error)> ValidateOptionsFile(
        DirectoryInfo rootPath,
        string? settingsOptionsPath)
    {
        var fileInfo = GetOptionsFile(rootPath, settingsOptionsPath);
        if (!fileInfo.Exists)
        {
            return (false, "File do not exist");
        }

        var options = await DeserializeFile(fileInfo);
        return options is null
            ? (false, "File is invalid")
            : (true, string.Empty);
    }

    private static FileInfo GetOptionsFile(
        DirectoryInfo rootPath,
        string? settingsOptionsPath)
    {
        var optionsPath = GetOptionsPath(rootPath, settingsOptionsPath);

        return optionsPath.EndsWith(".json", StringComparison.Ordinal)
            ? new FileInfo(optionsPath)
            : new FileInfo(Path.Combine(optionsPath, "atc-coding-rules-updater.json"));
    }

    private static string GetOptionsPath(
        DirectoryInfo rootPath,
        string? settingsOptionsPath)
        => string.IsNullOrEmpty(settingsOptionsPath)
            ? rootPath.FullName
            : settingsOptionsPath;

    private static async Task<OptionRoot?> DeserializeFile(
        FileInfo fileInfo)
    {
        var serializeOptions = JsonSerializerOptionsFactory.Create();
        using var stream = new StreamReader(fileInfo.FullName);
        var json = await stream.ReadToEndAsync();
        return JsonSerializer.Deserialize<OptionRoot>(json, serializeOptions);
    }
}
namespace Atc.CodingRules.Updater.CLI;

public static class OptionsHelper
{
    public static OptionRoot CreateDefault(
        DirectoryInfo rootPath,
        string? settingsOptionsPath)
    {
        var optionsPath = settingsOptionsPath is null || string.IsNullOrEmpty(settingsOptionsPath)
            ? rootPath.FullName
            : settingsOptionsPath;

        var fileInfo = optionsPath.EndsWith(".json", StringComparison.Ordinal)
            ? new FileInfo(optionsPath)
            : new FileInfo(Path.Combine(optionsPath, "atc-coding-rules-updater.json"));

        var options = DeserializeFile(fileInfo);
        options.Mappings.ResolvePaths(new DirectoryInfo(optionsPath));
        return options;
    }

    private static OptionRoot DeserializeFile(
        FileInfo fileInfo)
    {
        var options = new OptionRoot();

        if (!fileInfo.Exists)
        {
            return options;
        }

        var serializeOptions = new JsonSerializerOptions();
        serializeOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        serializeOptions.WriteIndented = true;

        using var stream = new StreamReader(fileInfo.FullName);
        var json = stream.ReadToEnd();
        options = JsonSerializer.Deserialize<OptionRoot>(json, serializeOptions);

        return options;
    }
}
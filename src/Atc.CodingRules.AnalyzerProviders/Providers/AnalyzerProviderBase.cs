namespace Atc.CodingRules.AnalyzerProviders.Providers;

public abstract class AnalyzerProviderBase : IAnalyzerProvider
{
    private const string GitRawAtcAnalyzerProviderBaseRulesBasePath = "https://raw.githubusercontent.com/atc-net/atc-coding-rules-updater/main/AnalyzerProviderBaseRules/";
    private readonly ILogger logger;

    protected AnalyzerProviderBase(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual Uri? DocumentationLink { get; set; }

    public async Task<AnalyzerProviderBaseRuleData> CollectBaseRules(
        ProviderCollectingMode providerCollectingMode)
    {
        var data = CreateData();

        if (providerCollectingMode == ProviderCollectingMode.LocalCache)
        {
            var dataFromTemp = await ReadFromTempFolder(data);
            if (dataFromTemp is not null)
            {
                return dataFromTemp;
            }
        }

        if (providerCollectingMode != ProviderCollectingMode.ReCollect)
        {
            var dataFromGithub = await ReadFromGithub(data);
            if (dataFromGithub is not null)
            {
                await WriteToTempFolder(dataFromGithub);
                return dataFromGithub;
            }
        }

        await ReCollect(data);
        await WriteToTempFolder(data);

        return data;
    }

    protected abstract AnalyzerProviderBaseRuleData CreateData();

    protected abstract Task ReCollect(AnalyzerProviderBaseRuleData data);

    protected static async Task<AnalyzerProviderBaseRuleData?> ReadFromTempFolder(
        AnalyzerProviderBaseRuleData data)
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AtcAnalyzerProviderBaseRules");
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        var tempFile = Path.Combine(tempFolder, data.Name + ".json");
        var fileInfo = new FileInfo(tempFile);
        if (!fileInfo.Exists)
        {
            return null;
        }

        var fileAsJson = await File.ReadAllTextAsync(tempFile);
        return JsonSerializer.Deserialize<AnalyzerProviderBaseRuleData>(fileAsJson, AnalyzerProviderSerialization.JsonOptions);
    }

    protected static Task WriteToTempFolder(
        AnalyzerProviderBaseRuleData data)
    {
        if (!string.IsNullOrEmpty(data.ExceptionMessage))
        {
            return Task.CompletedTask;
        }

        if (data.Rules.Count == 0)
        {
            data.ExceptionMessage = "No rules found";
            return Task.CompletedTask;
        }

        var tempFolder = Path.Combine(Path.GetTempPath(), "AtcAnalyzerProviderBaseRules");
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        var tempFile = Path.Combine(tempFolder, data.Name + ".json");
        var json = JsonSerializer.Serialize(data, AnalyzerProviderSerialization.JsonOptions);
        return File.WriteAllTextAsync(tempFile, json);
    }

    protected Task<AnalyzerProviderBaseRuleData?> ReadFromGithub(
        AnalyzerProviderBaseRuleData data)
    {
        var rawGitData = HttpClientHelper.GetAsString(logger, GitRawAtcAnalyzerProviderBaseRulesBasePath + data.Name + ".json");
        return Task.FromResult(string.IsNullOrEmpty(rawGitData)
            ? null
            : JsonSerializer.Deserialize<AnalyzerProviderBaseRuleData>(rawGitData, AnalyzerProviderSerialization.JsonOptions)!);
    }
}
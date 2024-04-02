namespace Atc.CodingRules.AnalyzerProviders.Providers;

public abstract class AnalyzerProviderBase : IAnalyzerProvider
{
    private const string GitRawAtcAnalyzerProviderBaseRulesBasePath = Constants.GitRawContentUrl + "/atc-net/atc-coding-rules-updater/main/AnalyzerProviderBaseRules/";
    private readonly ILogger logger;
    private readonly bool logWithAnsiConsoleMarkup;

    protected AnalyzerProviderBase(
        ILogger logger,
        bool logWithAnsiConsoleMarkup)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.logWithAnsiConsoleMarkup = logWithAnsiConsoleMarkup;
    }

    public virtual Uri? DocumentationLink { get; set; }

    public async Task<AnalyzerProviderBaseRuleData> CollectBaseRules(
        ProviderCollectingMode providerCollectingMode)
    {
        var data = CreateData();

        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace($"     [green]{data.Name}[/] collect base rules");

        if (providerCollectingMode == ProviderCollectingMode.LocalCache)
        {
            var dataFromTemp = await ReadFromTempFolder(data);
            if (dataFromTemp is not null)
            {
                StopTheStopwatchAndLog(stopwatch, data.Name, providerCollectingMode);

                return dataFromTemp;
            }
        }

        if (providerCollectingMode != ProviderCollectingMode.ReCollect)
        {
            var dataFromGithub = await ReadFromGithub(data);
            if (dataFromGithub is not null)
            {
                await WriteToTempFolder(dataFromGithub);

                StopTheStopwatchAndLog(stopwatch, data.Name, providerCollectingMode);

                return dataFromGithub;
            }
        }

        await ReCollect(data);
        await WriteToTempFolder(data);

        StopTheStopwatchAndLog(stopwatch, data.Name, providerCollectingMode);

        return data;
    }

    public void Cleanup()
    {
        var data = CreateData();

        var tempFolder = Path.Combine(Path.GetTempPath(), "AtcAnalyzerProviderBaseRules");
        if (!Directory.Exists(tempFolder))
        {
            return;
        }

        var tempFile = Path.Combine(tempFolder, data.Name + ".json");
        if (!File.Exists(tempFile))
        {
            return;
        }

        File.Delete(tempFile);
        logger.LogInformation($"File is deleted: {tempFile}");
    }

    protected abstract AnalyzerProviderBaseRuleData CreateData();

    protected abstract Task ReCollect(AnalyzerProviderBaseRuleData data);

    protected static async Task<AnalyzerProviderBaseRuleData?> ReadFromTempFolder(
        AnalyzerProviderBaseRuleData data)
    {
        ArgumentNullException.ThrowIfNull(data);

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
        ArgumentNullException.ThrowIfNull(data);

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
        ArgumentNullException.ThrowIfNull(data);

        var url = GitRawAtcAnalyzerProviderBaseRulesBasePath + data.Name + ".json";
        var displayName = url.Replace(Constants.GitRawContentUrl, Constants.GitHubPrefix, StringComparison.Ordinal);
        try
        {
            var rawGitData = HttpClientHelper.GetAsString(
                logger,
                url,
                displayName);
            return Task.FromResult(string.IsNullOrEmpty(rawGitData)
                ? null
                : JsonSerializer.Deserialize<AnalyzerProviderBaseRuleData>(rawGitData, AnalyzerProviderSerialization.JsonOptions)!);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return Task.FromResult<AnalyzerProviderBaseRuleData?>(null);
            }

            throw;
        }
    }

    private void StopTheStopwatchAndLog(
        Stopwatch stopwatch,
        string providerName,
        ProviderCollectingMode providerCollectingMode)
    {
        stopwatch.Stop();
        logger.LogTrace(logWithAnsiConsoleMarkup
            ? $"     [green]{providerName}[/] collect base rules by collecting mode: [green]{providerCollectingMode}[/] - time: [green]{stopwatch.Elapsed.GetPrettyTime()}[/]"
            : $"     {providerName} collect base rules by collecting mode: {providerCollectingMode} - time: {stopwatch.Elapsed.GetPrettyTime()}");
    }
}
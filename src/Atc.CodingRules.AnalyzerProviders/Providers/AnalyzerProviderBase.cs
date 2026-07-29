namespace Atc.CodingRules.AnalyzerProviders.Providers;

public abstract class AnalyzerProviderBase : IAnalyzerProvider
{
    private static readonly string GitRawAtcAnalyzerProviderBaseRulesBasePath = Constants.GitRawContentUrl + "/atc-net/atc-coding-rules-updater/main/AnalyzerProviderBaseRules/";
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

    /// <summary>
    /// The provider's name, derived from the same <see cref="CreateData"/> the collection path
    /// uses, so there is exactly one place per provider that defines it.
    /// </summary>
    public string ProviderName => CreateData().Name;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Falling back to a prior snapshot is preferable to crashing the run on a transient scrape failure.")]
    public async Task<AnalyzerProviderBaseRuleData> CollectBaseRules(
        ProviderCollectingMode providerCollectingMode)
    {
        var data = CreateData();

        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace($"     {Colored(data.Name, "green")} collect base rules");

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

        try
        {
            await ReCollect(data);
        }
        catch (Exception ex)
        {
            data.ExceptionMessage = ex.Message;
        }

        // If ReCollect produced a usable result, persist it; otherwise fall back to a prior on-disk
        // snapshot if one exists. This keeps the run usable when an upstream documentation page
        // changes layout or a transient failure leaves us with empty/exception-only data.
        if (string.IsNullOrEmpty(data.ExceptionMessage) && data.Rules.Count > 0)
        {
            await WriteToTempFolder(data);
        }
        else
        {
            var snapshot = await ReadFromTempFolder(data);
            if (snapshot is not null)
            {
                logger.LogWarning($"     {Colored(data.Name, "yellow")} collect failed; using prior cached snapshot. Reason: {data.ExceptionMessage ?? "no rules collected"}");
                StopTheStopwatchAndLog(stopwatch, data.Name, providerCollectingMode);
                return snapshot;
            }
        }

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

    protected static Task WriteToTempFolder(AnalyzerProviderBaseRuleData data)
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
        logger.LogTrace(
            $"     {Colored(providerName, "green")} collect base rules by collecting mode: {Colored(providerCollectingMode.ToString(), "green")} - time: {Colored(stopwatch.Elapsed.GetPrettyTime(), "green")}");
    }

    /// <summary>
    /// Wraps <paramref name="text"/> in a Spectre colour tag, or returns it untouched when the
    /// caller asked for plain output. Machine-readable modes such as
    /// <c>analyzer-providers collect --json</c> rely on this.
    /// </summary>
    private string Colored(
        string text,
        string color)
        => logWithAnsiConsoleMarkup
            ? $"[{color}]{text}[/]"
            : text;
}
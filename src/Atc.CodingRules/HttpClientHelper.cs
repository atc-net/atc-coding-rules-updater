namespace Atc.CodingRules;

/// <summary>
/// Synchronous (over async) HTTP downloader with a process-lifetime URL→content cache,
/// retry-with-backoff, and a clean 404 short-circuit.
/// </summary>
/// <remarks>
/// <para>
/// The cache is intentionally simple: <see cref="ConcurrentDictionary{TKey,TValue}"/> + a
/// final <c>GetOrAdd</c>. There is a benign race when two callers ask for the same URL at
/// the same time — both can complete the network fetch before either writes the cache.
/// <c>GetOrAdd</c> resolves the data race (only one value is kept), so callers see consistent
/// content; the cost is one redundant HTTP request in the rare concurrent case. Acceptable
/// because the existing call paths are sequential. If true concurrent dedup matters in future,
/// switch to <c>Lazy&lt;Task&lt;string&gt;&gt;</c> values keyed by URL.
/// </para>
/// </remarks>
public static class HttpClientHelper
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan[] BackoffDelays =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(600),
    ];

    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.Ordinal);
    private static readonly HttpClient SharedClient = new();

    public static string GetAsString(
        ILogger logger,
        string url,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var cacheValue = Cache.GetValueOrDefault(url);
        if (cacheValue is not null)
        {
            return cacheValue;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = url;
        }

        var response = TryDownloadWithRetry(logger, url, displayName, cancellationToken);
        if (response is null)
        {
            return string.Empty;
        }

        return Cache.GetOrAdd(url, response);
    }

    private static string? TryDownloadWithRetry(
        ILogger logger,
        string url,
        string displayName,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return Download(logger, url, displayName, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // 404 is an expected outcome when an upstream resource has not been published yet
                // (for example, an editor-config for a project framework that hasn't been added to
                // the distribution). Don't retry; don't cache; let the caller treat it as missing.
                return null;
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.ProtocolError &&
                                          ex.Message.Contains("404", StringComparison.Ordinal))
            {
                return null;
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                logger.LogTrace($"     Download attempt {attempt}/{MaxAttempts} failed: {ex.Message}");
            }
            catch (WebException ex) when (attempt < MaxAttempts)
            {
                logger.LogTrace($"     Download attempt {attempt}/{MaxAttempts} failed: {ex.GetMessage()}");
            }
            catch (TaskCanceledException ex) when (attempt < MaxAttempts)
            {
                logger.LogTrace($"     Download attempt {attempt}/{MaxAttempts} timed out: {ex.Message}");
            }

            // Sleep between attempts; the user's cancellation token short-circuits the wait.
            TaskHelper.RunSync(async () => await Task.Delay(BackoffDelays[attempt - 1], cancellationToken));
        }

        // Unreachable: on the final attempt the exception is not caught (the `when` filter excludes
        // the last attempt) and propagates out of this method directly.
        throw new InvalidOperationException("Download retry loop exited without returning or throwing.");
    }

    private static string Download(
        ILogger logger,
        string url,
        string displayName,
        CancellationToken cancellationToken)
    {
        var response = string.Empty;
        TaskHelper.RunSync(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogTrace($"     Download from: [link={url}]{displayName}[/]");

            var uri = new Uri(url);
            response = await SharedClient.GetStringAsync(uri, cancellationToken);

            stopwatch.Stop();
            logger.LogTrace($"     Download time: {stopwatch.Elapsed.GetPrettyTime()}");
        });
        return response;
    }
}
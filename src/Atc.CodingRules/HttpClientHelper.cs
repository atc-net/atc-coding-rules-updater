namespace Atc.CodingRules;

public static class HttpClientHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new (StringComparer.Ordinal);

    public static string GetAsString(
        ILogger logger,
        string url,
        CancellationToken cancellationToken = default)
    {
        var cacheValue = Cache.GetValueOrDefault(url);
        if (cacheValue is not null)
        {
            return cacheValue;
        }

        try
        {
            var response = string.Empty;
            TaskHelper.RunSync(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                logger.LogTrace($"     Download from: {url}");

                var uri = new Uri(url);
                using var client = new HttpClient();
                response = await client.GetStringAsync(uri, cancellationToken);

                stopwatch.Stop();
                logger.LogTrace($"     Download time: {stopwatch.Elapsed.GetPrettyTime()}");
            });

            return Cache.GetOrAdd(url, response);
        }
        catch (WebException ex)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError &&
                ex.Message.Contains("404", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            logger.LogTrace($"     Download error: {ex.GetMessage()}");
            throw;
        }
    }
}
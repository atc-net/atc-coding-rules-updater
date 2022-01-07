using System.Diagnostics;

namespace Atc.CodingRules;

public static class HttpClientHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new (StringComparer.Ordinal);

    public static string GetRawFile(ILogger logger, string rawFileUrl)
    {
        try
        {
            var response = string.Empty;
            TaskHelper.RunSync(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                logger.LogTrace($"     Download from: {rawFileUrl}");

                using var client = new HttpClient();
                response = await client.GetStringAsync(rawFileUrl);

                stopwatch.Stop();
                logger.LogTrace($"     Download time: {stopwatch.Elapsed.GetPrettyTime()}");
            });

            return Cache.GetOrAdd(rawFileUrl, response);
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
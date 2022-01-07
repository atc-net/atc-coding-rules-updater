namespace Atc.CodingRules;

public static class HttpClientHelper
{
    private static readonly ConcurrentDictionary<string, string> Cache = new (StringComparer.Ordinal);

    public static string GetRawFile(string rawFileUrl)
    {
        try
        {
            var response = string.Empty;
            TaskHelper.RunSync(async () =>
            {
                using var client = new HttpClient();
                response = await client.GetStringAsync(rawFileUrl);
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

            throw;
        }
    }
}
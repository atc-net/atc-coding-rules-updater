namespace Atc.CodingRules;

public static class AtcApiNugetClientHelper
{
    private const string BaseAddress = "https://atc-api.azurewebsites.net/nuget-search";
    private static readonly ConcurrentDictionary<string, Version> Cache = new (StringComparer.Ordinal);

    public static Version? GetLatestVersionForPackageId(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        var cacheValue = Cache.GetValueOrDefault(packageId);
        if (cacheValue is not null)
        {
            return cacheValue;
        }

        try
        {
            var response = string.Empty;
            TaskHelper.RunSync(async () =>
            {
                using var client = new HttpClient();
                response = await client.GetStringAsync($"{BaseAddress}/package?packageId={packageId}", cancellationToken);
            });

            if (string.IsNullOrEmpty(response) ||
                !Version.TryParse(response, out var version))
            {
                return null;
            }

            Cache.GetOrAdd(packageId, version);
            return version;
        }
        catch
        {
            return null;
        }
    }

    public static Version? GetLatestVersionForPackageId(
        ILogger logger,
        string packageId,
        CancellationToken cancellationToken = default)
    {
        var cacheValue = Cache.GetValueOrDefault(packageId);
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
                logger.LogTrace($"     Get newest version for:  {packageId}");

                using var client = new HttpClient();
                response = await client.GetStringAsync($"{BaseAddress}/package?packageId={packageId}", cancellationToken);

                stopwatch.Stop();
                logger.LogTrace($"     Get newest version time: {stopwatch.Elapsed.GetPrettyTime()}");
            });

            if (string.IsNullOrEmpty(response) ||
                !Version.TryParse(response, out var version))
            {
                return null;
            }

            Cache.GetOrAdd(packageId, version);
            return version;
        }
        catch (WebException ex)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError &&
                ex.Message.Contains("404", StringComparison.Ordinal))
            {
                return null;
            }

            logger.LogTrace($"     Get newest version error: {ex.GetMessage()}");
            throw;
        }
    }
}
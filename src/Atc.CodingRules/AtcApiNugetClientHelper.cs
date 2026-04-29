namespace Atc.CodingRules;

public static class AtcApiNugetClientHelper
{
    private const string BaseAddress = "https://atcnet-api-prod-api-ca-01.greenhill-862ffb7c.swedencentral.azurecontainerapps.io/nuget-search";
    private static readonly ConcurrentDictionary<string, Version> Cache = new(StringComparer.Ordinal);
    private static readonly HttpClient SharedClient = new();

    public static Version? GetLatestVersionForPackageId(
        string packageId,
        CancellationToken cancellationToken = default)
        => GetLatestVersionCore(logger: null, packageId, cancellationToken);

    public static Version? GetLatestVersionForPackageId(
        ILogger logger,
        string packageId,
        CancellationToken cancellationToken = default)
        => GetLatestVersionCore(logger, packageId, cancellationToken);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Failures should not crash the caller; cache miss simply returns null.")]
    private static Version? GetLatestVersionCore(
        ILogger? logger,
        string packageId,
        CancellationToken cancellationToken)
    {
        var cacheValue = Cache.GetValueOrDefault(packageId);
        if (cacheValue is not null)
        {
            return cacheValue;
        }

        try
        {
            var response = string.Empty;
            var uri = new Uri($"{BaseAddress}/package?packageId={packageId}");
            TaskHelper.RunSync(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                logger?.LogTrace($"     Get newest version for:  {packageId}");

                response = await SharedClient.GetStringAsync(uri, cancellationToken);

                stopwatch.Stop();
                logger?.LogTrace($"     Get newest version time: {stopwatch.Elapsed.GetPrettyTime()}");
            });

            if (string.IsNullOrEmpty(response) ||
                !Version.TryParse(response, out var version))
            {
                return null;
            }

            return Cache.GetOrAdd(packageId, version);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger?.LogTrace($"     Get newest version error: {ex.Message}");
            return null;
        }
        catch (WebException ex)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError &&
                ex.Message.Contains("404", StringComparison.Ordinal))
            {
                return null;
            }

            logger?.LogTrace($"     Get newest version error: {ex.GetMessage()}");
            return null;
        }
        catch
        {
            return null;
        }
    }
}
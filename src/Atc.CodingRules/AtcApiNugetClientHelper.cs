namespace Atc.CodingRules;

public static class AtcApiNugetClientHelper
{
    private const string BaseAddress = "https://atcnet-api-prod-api-ca-01.greenhill-862ffb7c.swedencentral.azurecontainerapps.io/nuget-search";
    private static readonly ConcurrentDictionary<string, Version> Cache = new(StringComparer.Ordinal);
    private static readonly HttpClient SharedClient = new();

    /// <summary>
    /// Builds the version-lookup URI for <paramref name="packageId"/>.
    /// </summary>
    /// <param name="packageId">The NuGet package id.</param>
    /// <param name="forceRefresh">
    /// When <c>true</c>, appends <c>invalidateCache=true</c> so the API re-reads from nuget.org
    /// instead of serving its 12-hour in-memory cache.
    /// </param>
    public static Uri BuildPackageUri(
        string packageId,
        bool forceRefresh)
    {
        var query = $"?packageId={Uri.EscapeDataString(packageId)}";
        if (forceRefresh)
        {
            query += "&invalidateCache=true";
        }

        return new Uri($"{BaseAddress}/package{query}");
    }

    /// <summary>
    /// Resolves <paramref name="packageIds"/> concurrently into the process cache, so the
    /// sequential lookups that follow are served from memory.
    /// </summary>
    /// <remarks>
    /// This is where the wall-clock actually goes: a typical run resolves ~15 packages, and the
    /// per-package round-trip dominates the distribution downloads by roughly five to one.
    /// Failures are not cached, so an unresolved package is simply retried by the caller.
    /// </remarks>
    public static void Prefetch(
        ILogger logger,
        IEnumerable<string> packageIds,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageIds);

        var pending = packageIds
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.Ordinal)
            .Where(x => !Cache.ContainsKey(x))
            .ToArray();

        if (pending.Length <= 1)
        {
            return;
        }

        TaskHelper.RunSync(async () =>
            await Task.WhenAll(pending.Select(packageId => Task.Run(
                () => GetLatestVersionCore(logger, packageId, forceRefresh, cancellationToken),
                cancellationToken))));
    }

    public static Version? GetLatestVersionForPackageId(
        string packageId,
        CancellationToken cancellationToken = default)
        => GetLatestVersionCore(logger: null, packageId, forceRefresh: false, cancellationToken);

    public static Version? GetLatestVersionForPackageId(
        ILogger logger,
        string packageId,
        CancellationToken cancellationToken = default)
        => GetLatestVersionCore(logger, packageId, forceRefresh: false, cancellationToken);

    /// <summary>
    /// Resolves the newest version for <paramref name="packageId"/>, optionally instructing the
    /// ATC API to bypass its own cache first.
    /// </summary>
    public static Version? GetLatestVersionForPackageId(
        ILogger logger,
        string packageId,
        bool forceRefresh,
        CancellationToken cancellationToken = default)
        => GetLatestVersionCore(logger, packageId, forceRefresh, cancellationToken);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Failures should not crash the caller; cache miss simply returns null.")]
    private static Version? GetLatestVersionCore(
        ILogger? logger,
        string packageId,
        bool forceRefresh,
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
            var uri = BuildPackageUri(packageId, forceRefresh);
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
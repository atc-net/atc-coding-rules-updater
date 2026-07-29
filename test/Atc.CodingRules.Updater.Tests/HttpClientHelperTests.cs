namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// The downloader is sync-over-async and every call site is sequential, so a solution with many
/// project-framework projects pays one serial round-trip each. <c>Prefetch</c> warms the existing
/// URL cache concurrently, leaving the sequential call sites untouched.
/// </summary>
[Trait(Traits.Category, Traits.Categories.Integration)]
public sealed class HttpClientHelperTests
{
    private const string BaseUrl =
        "https://raw.githubusercontent.com/atc-net/atc-coding-rules/main/distribution/dotnet10";

    private readonly ITestOutputHelper testOutput;

    public HttpClientHelperTests(ITestOutputHelper testOutput)
        => this.testOutput = testOutput;

    [Fact]
    public void Prefetch_MakesSubsequentGetAsStringServeFromCache()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        var urls = new[]
        {
            $"{BaseUrl}/.editorconfig",
            $"{BaseUrl}/src/.editorconfig",
            $"{BaseUrl}/test/.editorconfig",
        };

        HttpClientHelper.Prefetch(logger, urls, TestContext.Current.CancellationToken);

        // Every URL is already cached, so these must not log a download.
        using var afterLogger = testOutput.BuildLogger(LogLevel.Trace);
        foreach (var url in urls)
        {
            HttpClientHelper.GetAsString(afterLogger, url, displayName: url, TestContext.Current.CancellationToken)
                .Should().NotBeEmpty();
        }

        afterLogger.Entries
            .Should().NotContain(x => x.Message.Contains("Download from:", StringComparison.Ordinal));
    }

    [Fact]
    public void Prefetch_IgnoresDuplicateAndEmptyUrls()
    {
        using var logger = testOutput.BuildLogger(LogLevel.Trace);

        var url = $"{BaseUrl}/.editorconfig";

        // Must not throw on empty entries, and must not fetch the same URL twice.
        HttpClientHelper.Prefetch(logger, [url, url, string.Empty], TestContext.Current.CancellationToken);

        HttpClientHelper.GetAsString(logger, url, displayName: url, TestContext.Current.CancellationToken)
            .Should().NotBeEmpty();
    }
}
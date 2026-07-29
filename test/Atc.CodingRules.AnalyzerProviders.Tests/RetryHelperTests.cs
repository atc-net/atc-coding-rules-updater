namespace Atc.CodingRules.AnalyzerProviders.Tests;

public sealed class RetryHelperTests
{
    private const int NoDelay = 1;

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesUntilResultIsValid()
    {
        var attempts = 0;

        var actual = await RetryHelper.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                return Task.FromResult(attempts);
            },
            isValid: x => x >= 3,
            delayBetweenRetriesMs: NoDelay);

        actual.Should().Be(3);
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_DoesNotRetry_WhenFirstResultIsValid()
    {
        var attempts = 0;

        var actual = await RetryHelper.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                return Task.FromResult("ok");
            },
            isValid: _ => true,
            delayBetweenRetriesMs: NoDelay);

        actual.Should().Be("ok");
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ReturnsLastResult_WhenNeverValid()
    {
        var attempts = 0;

        // The caller's own assertions should report the real problem, so the last (invalid)
        // result is returned rather than an exception about retrying.
        var actual = await RetryHelper.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                return Task.FromResult(attempts);
            },
            isValid: _ => false,
            maxRetries: 3,
            delayBetweenRetriesMs: NoDelay);

        actual.Should().Be(3);
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesOnException_AndReturnsLaterSuccess()
    {
        var attempts = 0;

        var actual = await RetryHelper.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new InvalidOperationException("transient");
                }

                return Task.FromResult("recovered");
            },
            isValid: _ => true,
            delayBetweenRetriesMs: NoDelay);

        actual.Should().Be("recovered");
        attempts.Should().Be(2);
    }
}
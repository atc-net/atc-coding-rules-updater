namespace Atc.CodingRules.AnalyzerProviders.Tests;

/// <summary>
/// Helper class for retrying flaky tests that depend on external services.
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// Executes an async action with retry logic.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default is 3).</param>
    /// <param name="delayBetweenRetriesMs">Delay in milliseconds between retries (default is 1000ms).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Retry logic needs to catch all exceptions.")]
    public static async Task ExecuteWithRetryAsync(
        Func<Task> action,
        int maxRetries = 3,
        int delayBetweenRetriesMs = 1000)
    {
        ArgumentNullException.ThrowIfNull(action);

        var attempts = 0;
        while (true)
        {
            try
            {
                attempts++;
                await action();
                return;
            }
            catch when (attempts < maxRetries)
            {
                await Task.Delay(delayBetweenRetriesMs);
            }
        }
    }

    /// <summary>
    /// Executes an async function with retry logic, retrying when it throws <em>or</em> when the
    /// returned value fails <paramref name="isValid"/>.
    /// </summary>
    /// <remarks>
    /// The result-validating overload exists because
    /// <c>AnalyzerProviderBase.CollectBaseRules</c> catches its own exceptions and returns
    /// degraded data rather than throwing. With the exception-only overload the retry could
    /// never fire for a partial scrape, and the caller's assertions — which run after the retry
    /// has already given up — failed on the first bad result.
    /// </remarks>
    /// <typeparam name="T">The type produced by <paramref name="action"/>.</typeparam>
    /// <param name="action">The async function to execute.</param>
    /// <param name="isValid">Predicate deciding whether a result is usable; a <c>false</c> result triggers a retry.</param>
    /// <param name="maxRetries">Maximum number of attempts (default is 3).</param>
    /// <param name="delayBetweenRetriesMs">Delay in milliseconds between attempts (default is 1000ms).</param>
    /// <returns>
    /// The first valid result, or the last result obtained once the attempts are exhausted, so
    /// that the caller's own assertions report the actual problem.
    /// </returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Retry logic needs to catch all exceptions.")]
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        Func<T, bool> isValid,
        int maxRetries = 3,
        int delayBetweenRetriesMs = 1000)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(isValid);

        var attempts = 0;
        while (true)
        {
            attempts++;

            try
            {
                var result = await action();
                if (isValid(result) || attempts >= maxRetries)
                {
                    return result;
                }
            }
            catch when (attempts < maxRetries)
            {
                // Fall through to the delay and try again.
            }

            await Task.Delay(delayBetweenRetriesMs);
        }
    }
}
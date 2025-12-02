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
}
namespace Atc.CodingRules;

/// <summary>
/// Shared constants used across the updater. Base URLs honour environment-variable overrides so a
/// run can be pointed at a mirror or proxy without code changes (useful for air-gapped CI).
/// </summary>
public static class Constants
{
    /// <summary>
    /// Environment variable: when set and non-empty, replaces the default
    /// <c>https://raw.githubusercontent.com</c> base used for every download.
    /// </summary>
    public const string GitRawContentUrlEnvVar = "ATC_CODING_RULES_RAW_BASE";

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Default base; can be overridden via env var ATC_CODING_RULES_RAW_BASE.")]
    private const string DefaultGitRawContentUrl = "https://raw.githubusercontent.com";

    /// <summary>Decorative prefix used in console output to mark a GitHub link.</summary>
    public const string GitHubPrefix = "[silver][[GitHub]][/] ";

    /// <summary>
    /// Raw GitHub content base URL. Defaults to <c>https://raw.githubusercontent.com</c>;
    /// override at runtime by setting the <c>ATC_CODING_RULES_RAW_BASE</c> environment variable
    /// (e.g. to a mirror like <c>https://my-internal-mirror</c>). Trailing slashes are trimmed.
    /// </summary>
    public static readonly string GitRawContentUrl = ResolveGitRawContentUrl();

    private static string ResolveGitRawContentUrl()
    {
        var fromEnv = Environment.GetEnvironmentVariable(GitRawContentUrlEnvVar);
        if (string.IsNullOrWhiteSpace(fromEnv))
        {
            return DefaultGitRawContentUrl;
        }

        return fromEnv.TrimEnd('/');
    }
}
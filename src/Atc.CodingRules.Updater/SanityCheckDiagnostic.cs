namespace Atc.CodingRules.Updater;

/// <summary>
/// One diagnostic emitted by <see cref="ProjectSanityCheckHelper"/> when validating a project tree
/// before the updater touches files. Used by both the human-readable logger output and the
/// machine-readable <c>sanity-check --json</c> output.
/// </summary>
public sealed class SanityCheckDiagnostic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SanityCheckDiagnostic"/> class.
    /// </summary>
    /// <param name="severity">Severity (warning vs. error).</param>
    /// <param name="code">Stable identifier — use the same string when consuming the JSON output programmatically.</param>
    /// <param name="message">Human-readable description.</param>
    /// <param name="filePath">Optional file the diagnostic refers to.</param>
    public SanityCheckDiagnostic(
        SanityCheckSeverity severity,
        string code,
        string message,
        string? filePath = null)
    {
        Severity = severity;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        FilePath = filePath;
    }

    /// <summary>Severity of this diagnostic.</summary>
    public SanityCheckSeverity Severity { get; }

    /// <summary>Stable identifier (e.g. <c>MissingOrganizationName</c>, <c>EnableNETAnalyzers</c>).</summary>
    public string Code { get; }

    /// <summary>Human-readable description.</summary>
    public string Message { get; }

    /// <summary>File the diagnostic refers to, when applicable.</summary>
    public string? FilePath { get; }
}
namespace Atc.CodingRules.Updater;

/// <summary>
/// What happened to a single rule file during a run.
/// </summary>
/// <remarks>
/// There are no separate dry-run values. Under <c>--dry-run</c> the same outcome is reported for
/// what <em>would</em> have happened, and the caller pairs it with the run's dry-run flag.
/// </remarks>
public enum FileUpdateOutcome
{
    /// <summary>The local file already matched; nothing was written.</summary>
    Unchanged,

    /// <summary>The file did not exist and was written from the distribution.</summary>
    Created,

    /// <summary>The file existed and its content changed (an .editorconfig merge, or a package bump).</summary>
    Updated,

    /// <summary>Nothing was done because the upstream content was missing or empty.</summary>
    Skipped,
}
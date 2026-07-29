namespace Atc.CodingRules.Updater.CLI.Models;

/// <summary>
/// Machine-readable result of a <c>run</c>, emitted by <c>--json</c> and used by
/// <c>--failOnChanges</c>.
/// </summary>
public sealed class RunSummary
{
    /// <summary>When <c>true</c>, the outcomes describe what would have happened.</summary>
    public bool DryRun { get; set; }

    /// <summary>Every rule file the run considered.</summary>
    public Collection<RunFileResult> Files { get; } = [];

    /// <summary>Every package version bump applied, across all props files.</summary>
    public Collection<RunPackageBump> PackageBumps { get; } = [];

    /// <summary>Reported-but-not-applied differences against the distribution.</summary>
    public Collection<RunDriftEntry> Drift { get; } = [];

    /// <summary>
    /// <c>true</c> when at least one file was created or updated. Drift alone does not count — it
    /// is never applied, so it is not something a re-run would resolve.
    /// </summary>
    public bool HasChanges
        => Files.Any(x => x.Outcome is FileUpdateOutcome.Created or FileUpdateOutcome.Updated);
}
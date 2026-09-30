namespace Atc.CodingRules.Updater.CLI.Models;

/// <summary>
/// Machine-readable result of a <c>run</c>, emitted by <c>--json</c> and used by
/// <c>--failOnChanges</c>.
/// </summary>
public sealed class RunSummary
{
    /// <summary>When <c>true</c>, the outcomes describe what would have happened.</summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// <c>true</c> when the run used the Central Package Management path; <c>null</c> when it did
    /// not. It is left out of the JSON when <c>null</c>, so repositories that do not use CPM get
    /// exactly the output they got before.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? CentralPackageManagement { get; set; }

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
namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// The result of <see cref="CpmMigrationExecutor.ExecuteAsync"/>.
/// </summary>
/// <param name="Outcome">How the run ended.</param>
/// <param name="Plan">The plan, including any blockers found before or during the run.</param>
/// <param name="Differences">What restore showed after writing, when the migration was rolled back.</param>
public sealed record CpmMigrationResult(
    CpmMigrationOutcome Outcome,
    CpmMigrationPlan Plan,
    IReadOnlyList<string> Differences);
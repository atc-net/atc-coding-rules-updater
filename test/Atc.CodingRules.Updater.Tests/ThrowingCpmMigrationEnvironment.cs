namespace Atc.CodingRules.Updater.Tests;

/// <summary>
/// An <see cref="ICpmMigrationEnvironment"/> for tests that must never reach git or
/// <c>dotnet restore</c>: any call fails the test.
/// </summary>
internal sealed class ThrowingCpmMigrationEnvironment : ICpmMigrationEnvironment
{
    public Task<bool?> IsWorkingTreeCleanAsync(
        DirectoryInfo projectPath,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Not expected in this test.");

    public Task<RestoreSnapshot> RestoreAsync(
        DirectoryInfo projectPath,
        string? restoreTarget,
        string configuration,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Not expected in this test.");
}
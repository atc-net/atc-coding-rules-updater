namespace Atc.CodingRules.Updater.CLI.Commands;

/// <summary>
/// <c>cpm migrate</c>: converts a repository to NuGet Central Package Management
/// (<c>Directory.Packages.props</c>).
/// </summary>
/// <remarks>
/// This is the only way the updater turns Central Package Management on; <c>run</c> never does.
/// See <see cref="CpmMigrationExecutor"/> for the safety checks around the migration.
/// </remarks>
public class CpmMigrateCommand(ILogger<CpmMigrateCommand> logger) : AsyncCommand<CpmMigrateCommandSettings>
{
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        CpmMigrateCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, new DotnetCpmMigrationEnvironment(), cancellationToken);
    }

    internal Task<int> ExecuteInternalAsync(
        CpmMigrateCommandSettings settings,
        ICpmMigrationEnvironment environment,
        CancellationToken cancellationToken)
        => CpmCommandHelper.ExecuteAsync(logger, settings, environment, cancellationToken);
}
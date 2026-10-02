namespace Atc.CodingRules.Updater.CLI.Commands;

/// <summary>
/// <c>cpm validate</c>: shows what <c>cpm migrate</c> would do and every blocker, without writing,
/// running git or restoring. The exit code is 1 when the migration is blocked, so CI can gate on it.
/// </summary>
public class CpmValidateCommand(ILogger<CpmValidateCommand> logger) : AsyncCommand<CpmCommandSettings>
{
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        CpmCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, new DotnetCpmMigrationEnvironment(), cancellationToken);
    }

    internal Task<int> ExecuteInternalAsync(
        CpmCommandSettings settings,
        ICpmMigrationEnvironment environment,
        CancellationToken cancellationToken)
        => CpmCommandHelper.ExecuteAsync(logger, settings, environment, cancellationToken);
}
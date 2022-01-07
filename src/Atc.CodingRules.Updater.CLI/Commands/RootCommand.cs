namespace Atc.CodingRules.Updater.CLI.Commands;

public class RootCommand : AsyncCommand<RootCommandSettings>
{
    private readonly ILogger<RootCommand> logger;

    public RootCommand(ILogger<RootCommand> logger) => this.logger = logger;

    public override Task<int> ExecuteAsync(
        CommandContext context,
        RootCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(
        RootCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var outputRootPath = new DirectoryInfo(settings.OutputRootPath);
        var temporarySuppressionsPath = GetTemporarySuppressionsPath(settings);
        var optionsPath = GetOptionsPath(settings);
        var options = OptionsHelper.CreateDefault(outputRootPath, optionsPath);

        try
        {
            await ConfigHelper.HandleFiles(
                logger,
                outputRootPath,
                options,
                settings.UseTemporarySuppressions.GetValueOrDefault(),
                temporarySuppressionsPath,
                settings.TemporarySuppressionAsExcel.GetValueOrDefault(),
                GetBuildFile(outputRootPath, settings));
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.GetMessage()}");
            return ConsoleExitStatusCodes.Failure;
        }

        logger.LogInformation($"{EmojisConstants.Done} Done");
        return ConsoleExitStatusCodes.Success;
    }

    private static string GetOptionsPath(
        RootCommandSettings settings)
    {
        var optionsPath = string.Empty;
        if (settings.OptionsPath is not null && settings.OptionsPath.IsSet)
        {
            optionsPath = settings.OptionsPath.Value;
        }

        return optionsPath;
    }

    private static DirectoryInfo? GetTemporarySuppressionsPath(
        RootCommandSettings settings)
    {
        var temporarySuppressionsPath = string.Empty;
        if (settings.TemporarySuppressionsPath is not null && settings.TemporarySuppressionsPath.IsSet)
        {
            temporarySuppressionsPath = settings.TemporarySuppressionsPath.Value;
        }

        return !string.IsNullOrEmpty(temporarySuppressionsPath)
            ? new DirectoryInfo(temporarySuppressionsPath)
            : null;
    }

    private static FileInfo? GetBuildFile(
        DirectoryInfo rootPath,
        RootCommandSettings settings)
    {
        var buildFile = string.Empty;
        if (settings.BuildFile is not null && settings.BuildFile.IsSet)
        {
            buildFile = settings.BuildFile.Value;
        }

        if (!string.IsNullOrEmpty(buildFile))
        {
            return buildFile.Contains(':', StringComparison.Ordinal)
                ? new FileInfo(buildFile)
                : new FileInfo(Path.Combine(rootPath.FullName, buildFile));
        }

        return null;
    }
}
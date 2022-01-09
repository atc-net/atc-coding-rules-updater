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
        var optionsPath = GetOptionsPath(settings);
        var options = OptionsHelper.CreateDefault(outputRootPath, optionsPath);

        if (!options.HasMappingsPaths())
        {
            options.Mappings.Sample.Paths.Add(Path.Combine(outputRootPath.FullName, "sample"));
            options.Mappings.Src.Paths.Add(Path.Combine(outputRootPath.FullName, "src"));
            options.Mappings.Test.Paths.Add(Path.Combine(outputRootPath.FullName, "test"));
        }

        var solutionTarget = GetSolutionTarget(settings);
        if (!string.IsNullOrEmpty(solutionTarget))
        {
            options.SolutionTarget = solutionTarget;
        }

        if (settings.UseLatestMinorNugetVersion.HasValue)
        {
            options.UseLatestMinorNugetVersion = settings.UseLatestMinorNugetVersion.GetValueOrDefault();
        }

        if (settings.UseTemporarySuppressions.HasValue)
        {
            options.UseTemporarySuppressions = settings.UseTemporarySuppressions.GetValueOrDefault();
        }

        var temporarySuppressionsPath = GetTemporarySuppressionsPath(settings);
        if (temporarySuppressionsPath is not null &&
            temporarySuppressionsPath.Exists)
        {
            options.TemporarySuppressionsPath = temporarySuppressionsPath.FullName;
        }

        if (settings.TemporarySuppressionAsExcel.HasValue)
        {
            options.TemporarySuppressionAsExcel = settings.TemporarySuppressionAsExcel.GetValueOrDefault();
        }

        var buildFile = GetBuildFile(outputRootPath, settings);

        try
        {
            await ConfigHelper.HandleFiles(
                logger,
                outputRootPath,
                options,
                buildFile);
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

    private static string GetSolutionTarget(
        RootCommandSettings settings)
    {
        var optionsPath = string.Empty;
        if (settings.SolutionTarget is not null && settings.SolutionTarget.IsSet)
        {
            optionsPath = settings.SolutionTarget.Value;
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
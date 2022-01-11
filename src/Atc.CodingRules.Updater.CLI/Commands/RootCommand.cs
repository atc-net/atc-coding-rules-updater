using Spectre.Console;

namespace Atc.CodingRules.Updater.CLI.Commands;

public class RootCommand : AsyncCommand<RootCommandSettings>
{
    private readonly ILogger<RootCommand> logger;

    public RootCommand(ILogger<RootCommand> logger) => this.logger = logger;

    public override Task<int> ExecuteAsync(
        CommandContext context,
        RootCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(
        RootCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var outputRootPath = new DirectoryInfo(settings.OutputRootPath);
        var optionsPath = settings.GetOptionsPath();
        var options = await OptionsHelper.CreateDefault(outputRootPath, optionsPath);

        var solutionTarget = GetSolutionTarget(settings);
        if (solutionTarget is not null)
        {
            options.SolutionTarget = (SupportedSolutionTargetType)solutionTarget;
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
        if (buildFile is not null)
        {
            options.BuildFile = buildFile.FullName;
        }

        try
        {
            await ConfigHelper.HandleFiles(
                logger,
                outputRootPath,
                options);

            if (DirectoryBuildPropsHelper.HasFileInsertPlaceholderElement(outputRootPath, "OrganizationName", "insert organization name here"))
            {
                string organizationName = AnsiConsole.Ask<string>("What is the [green]Organization name[/]?");
                DirectoryBuildPropsHelper.UpdateFileInsertPlaceholderElement(logger, outputRootPath, "OrganizationName", "insert organization name here", organizationName);
            }

            if (DirectoryBuildPropsHelper.HasFileInsertPlaceholderElement(outputRootPath, "RepositoryName", "insert repository name here"))
            {
                string repositoryName = AnsiConsole.Ask<string>("What is the [green]Repository name[/]?");
                DirectoryBuildPropsHelper.UpdateFileInsertPlaceholderElement(logger, outputRootPath, "RepositoryName", "insert repository name here", repositoryName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.GetMessage()}");
            return ConsoleExitStatusCodes.Failure;
        }

        logger.LogInformation($"{EmojisConstants.Done} Done");
        return ConsoleExitStatusCodes.Success;
    }

    private static SupportedSolutionTargetType? GetSolutionTarget(
        RootCommandSettings settings)
        => settings.SolutionTarget.IsSet
            ? settings.SolutionTarget.Value
            : null;

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
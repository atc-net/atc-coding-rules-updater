// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.CodingRules.Updater.CLI.Commands;

[SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "OK.")]
public class RunCommand : AsyncCommand<RunCommandSettings>
{
    private readonly ILogger<RunCommand> logger;

    public RunCommand(
        ILogger<RunCommand> logger)
        => this.logger = logger;

    public override Task<int> ExecuteAsync(
        CommandContext context,
        RunCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK.")]
    private async Task<int> ExecuteInternalAsync(
        RunCommandSettings settings)
    {
        if (!NetworkInformationHelper.HasHttpConnection())
        {
            System.Console.WriteLine("This tool requires internet connection!");
            return ConsoleExitStatusCodes.Failure;
        }

        ConsoleHelper.WriteHeader();

        var projectPath = new DirectoryInfo(settings.ProjectPath);
        var options = await GetOptionsFromFileAndUserArguments(settings, projectPath);

        try
        {
            CodingRulesUpdaterVersionHelper.PrintUpdateInfoIfNeeded(logger);

            await ProjectHelper.HandleFiles(
                logger,
                projectPath,
                options);

            if (DirectoryBuildPropsHelper.HasFileInsertPlaceholderElement(projectPath, "OrganizationName", "insert organization name here"))
            {
                var organizationName = settings.OrganizationName is not null && settings.OrganizationName.IsSet
                    ? settings.OrganizationName.Value
                    : AnsiConsole.Ask<string>("What is the [green]Organization name[/]?");

                DirectoryBuildPropsHelper.UpdateFileInsertPlaceholderElement(logger, projectPath, "OrganizationName", "insert organization name here", organizationName);
            }

            if (DirectoryBuildPropsHelper.HasFileInsertPlaceholderElement(projectPath, "RepositoryName", "insert repository name here"))
            {
                var repositoryName = settings.RepositoryName is not null && settings.RepositoryName.IsSet
                    ? settings.RepositoryName.Value
                    : AnsiConsole.Ask<string>("What is the [green]Repository name[/]?");

                DirectoryBuildPropsHelper.UpdateFileInsertPlaceholderElement(logger, projectPath, "RepositoryName", "insert repository name here", repositoryName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"{EmojisConstants.Error} {ex.Message}");
            return ConsoleExitStatusCodes.Failure;
        }

        logger.LogInformation($"{EmojisConstants.Success} Done");
        return ConsoleExitStatusCodes.Success;
    }

    private static async Task<OptionsFile> GetOptionsFromFileAndUserArguments(
        RunCommandSettings settings,
        DirectoryInfo projectPath)
    {
        var optionsPath = settings.GetOptionsPath();
        var options = await OptionsHelper.CreateDefault(projectPath, optionsPath);
        options.Mappings.ResolvePaths(projectPath);

        var projectTarget = ProjectCommandSettings.GetProjectTarget(settings);
        if (projectTarget is not null)
        {
            options.ProjectTarget = (SupportedProjectTargetType)projectTarget;
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

        var buildFile = GetBuildFile(settings, projectPath);
        if (buildFile is not null)
        {
            options.BuildFile = buildFile.FullName;
        }

        return options;
    }

    private static DirectoryInfo? GetTemporarySuppressionsPath(
        RunCommandSettings settings)
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
        RunCommandSettings settings,
        DirectoryInfo projectPath)
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
                : new FileInfo(Path.Combine(projectPath.FullName, buildFile));
        }

        return null;
    }
}
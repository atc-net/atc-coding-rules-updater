using System;
using System.IO;
using System.Threading.Tasks;
using Atc.CodingRules.Updater.CLI.Commands.Settings;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Atc.CodingRules.Updater.CLI.Commands
{
    public class RootCommand : AsyncCommand<RootCommandSettings>
    {
        private readonly ILogger<RootCommand> logger;

        public RootCommand(ILogger<RootCommand> logger) => this.logger = logger;

        public override Task<int> ExecuteAsync(CommandContext context, RootCommandSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return ExecuteInternalAsync(settings);
        }

        private async Task<int> ExecuteInternalAsync(RootCommandSettings settings)
        {
            ConsoleHelper.WriteHeader();

            var outputRootPath = new DirectoryInfo(settings.OutputRootPath);
            var temporarySuppressionsPath = GetTemporarySuppressionsPath(settings);

            var optionsPath = string.Empty;
            if (settings.OptionsPath is not null && settings.OptionsPath.IsSet)
            {
                optionsPath = settings.OptionsPath.Value;
            }

            var options = OptionsHelper.CreateDefault(outputRootPath, optionsPath);

            try
            {
                await ConfigHelper.HandleFiles(
                    logger,
                    outputRootPath,
                    options,
                    UseTemporarySuppressions(settings),
                    temporarySuppressionsPath,
                    TemporarySuppressionAsExcel(settings),
                    GetBuildFile(outputRootPath, settings));
            }
            catch (Exception ex)
            {
                logger.LogError($"{EmojisConstants.Error} {ex.GetMessage(true, true)}");
                return ConsoleExitStatusCodes.Failure;
            }

            logger.LogInformation($"{EmojisConstants.Done} Done");

            return ConsoleExitStatusCodes.Success;
        }

        private static DirectoryInfo? GetTemporarySuppressionsPath(RootCommandSettings settings)
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

        private static bool UseTemporarySuppressions(RootCommandSettings settings)
        {
            var result = false;
            if (settings.UseTemporarySuppressions is not null && settings.UseTemporarySuppressions.IsSet)
            {
                result = settings.UseTemporarySuppressions.Value;
            }

            return result;
        }

        private static bool TemporarySuppressionAsExcel(RootCommandSettings settings)
        {
            var result = false;
            if (settings.TemporarySuppressionAsExcel is not null && settings.TemporarySuppressionAsExcel.IsSet)
            {
                result = settings.TemporarySuppressionAsExcel.Value;
            }

            return result;
        }

        private static FileInfo? GetBuildFile(DirectoryInfo rootPath, RootCommandSettings settings)
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
}
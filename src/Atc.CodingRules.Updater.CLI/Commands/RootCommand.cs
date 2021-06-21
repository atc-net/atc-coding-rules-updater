using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Atc.CodingRules.Updater.CLI.Commands.CommandOptions;
using Atc.Data.Models;
using McMaster.Extensions.CommandLineUtils;

namespace Atc.CodingRules.Updater.CLI.Commands
{
    public class RootCommand : BaseCommandOptions
    {
        private const string RawCodingRulesDistribution = "https://raw.githubusercontent.com/atc-net/atc-coding-rules/main/distribution";

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Can't change it do to interface signature.")]
        public async Task<int> OnExecute(CommandLineApplication configCmd)
        {
            if (configCmd is null)
            {
                throw new ArgumentNullException(nameof(configCmd));
            }

            if (CommandLineApplicationHelper.GetHelpMode(configCmd))
            {
                ConsoleHelper.WriteHelp(configCmd, "Please specify some options");
                return ExitStatusCodes.Failure;
            }

            ConsoleHelper.WriteHeader();
            var verboseMode = CommandLineApplicationHelper.GetVerboseMode(configCmd);
            var options = OptionsHelper.CreateDefault(configCmd);
            var rootPath = CommandLineApplicationHelper.GetRootPath(configCmd);
            var useTemporarySuppressions = CommandLineApplicationHelper.GetUseTemporarySuppressions(configCmd);
            var temporarySuppressionsPath = CommandLineApplicationHelper.GetTemporarySuppressionsPath(configCmd);
            var temporarySuppressionAsExcel = CommandLineApplicationHelper.GetTemporarySuppressionAsExcel(configCmd);
            var buildFile = CommandLineApplicationHelper.GetBuildFile(configCmd);
            var logItems = new List<LogKeyValueItem>();

            logItems.AddRange(
                await ConfigHelper.HandleFiles(
                    RawCodingRulesDistribution,
                    rootPath,
                    options,
                    useTemporarySuppressions,
                    temporarySuppressionsPath,
                    temporarySuppressionAsExcel,
                    buildFile));

            return ConsoleHelper.WriteLogItemsAndExit(logItems, verboseMode, "Update");
        }
    }
}
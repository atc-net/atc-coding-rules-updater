using System;
using System.Collections.Generic;
using Atc.CodingRules.Updater.CLI.Commands.Options;
using Atc.Data.Models;
using McMaster.Extensions.CommandLineUtils;

namespace Atc.CodingRules.Updater.CLI.Commands
{
    public class RootCommand : BaseCommandOptions
    {
        private const string RawCodingRulesDistribution = "https://raw.githubusercontent.com/atc-net/atc-coding-rules/main/distribution";

        public int OnExecute(CommandLineApplication configCmd)
        {
            if (configCmd == null)
            {
                throw new ArgumentNullException(nameof(configCmd));
            }

            ConsoleHelper.WriteHeader();
            var verboseMode = CommandLineApplicationHelper.GetVerboseMode(configCmd);
            var rootPath = CommandLineApplicationHelper.GetRootPath(configCmd);
            var logItems = new List<LogKeyValueItem>();

            logItems.AddRange(EditorConfigHelper.Update(RawCodingRulesDistribution, rootPath));

            return ConsoleHelper.WriteLogItemsAndExit(logItems, verboseMode, "Update");
        }
    }
}
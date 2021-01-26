using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Atc.Data.Models;
using McMaster.Extensions.CommandLineUtils;

namespace Atc.CodingRules.Updater.CLI
{
    public static class ConsoleHelper
    {
        public static void WriteHeader()
        {
            Console.WriteLine();
            Colorful.Console.WriteAscii(" ATC-Rules updater", Color.CornflowerBlue);
        }

        public static void WriteHelp(CommandLineApplication configCmd, string message)
        {
            if (configCmd == null)
            {
                throw new ArgumentNullException(nameof(configCmd));
            }

            WriteHeader();
            Console.WriteLine(message);
            Console.WriteLine();
            configCmd.ShowHelp();
        }

        public static void WriteLogItems(IList<LogKeyValueItem> logItems, bool verboseMode)
        {
            if (logItems is null)
            {
                throw new ArgumentNullException(nameof(logItems));
            }

            foreach (var logItem in logItems)
            {
                var sb = new StringBuilder();
                sb.Append(logItem.Key.StartsWith("--", StringComparison.Ordinal)
                    ? logItem.Key
                    : $"{logItem.Key} # {logItem.LogCategory}: ");
                if (!"#".Equals(logItem.Value, StringComparison.Ordinal))
                {
                    sb.Append($"{logItem.Value}");
                }

                if (!string.IsNullOrEmpty(logItem.Description))
                {
                    sb.Append($"- {logItem.Description}");
                }

                var message = sb.ToString();
                switch (logItem.LogCategory)
                {
                    case LogCategoryType.Error:
                        Colorful.Console.WriteLine(message, Color.Red);
                        break;
                    case LogCategoryType.Warning:
                        Colorful.Console.WriteLine(message, Color.Yellow);
                        break;
                    case LogCategoryType.Information:
                        Colorful.Console.WriteLine(message, Color.LightSkyBlue);
                        break;
                    case LogCategoryType.Debug:
                        if (verboseMode)
                        {
                            Colorful.Console.WriteLine(message, Color.Tan);
                        }

                        break;
                }
            }
        }

        public static int WriteLogItemsAndExit(IList<LogKeyValueItem> logItems, bool verboseMode, string area)
        {
            WriteLogItems(logItems, verboseMode);
            Console.WriteLine();
            if (logItems.Any(x => x.LogCategory == LogCategoryType.Error))
            {
                Colorful.Console.WriteLine($"{area} has errors.", Color.Red);
                return ExitStatusCodes.Failure;
            }

            Console.WriteLine();
            Colorful.Console.WriteLine($"{area} is OK.", Color.DarkGreen);
            return ExitStatusCodes.Success;
        }
    }
}
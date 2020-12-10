using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace Atc.CodingRules.Updater.CLI
{
    public static class CommandLineApplicationHelper
    {
        public static bool GetVerboseMode(CommandLineApplication configCmd)
        {
            return TryGetValueForParameter(configCmd, "verboseMode", "v", out string parameterValueResult) &&
                   bool.TryParse(parameterValueResult, out bool result) &&
                   result;
        }

        public static DirectoryInfo GetRootPath(CommandLineApplication configCmd)
        {
            return new DirectoryInfo(GetValueForParameter(configCmd, "outputRootPath", "r"));
        }

        private static string GetValueForParameter(CommandLineApplication configCmd, string parameterName, string? shortParameterName = null)
        {
            if (TryGetValueForParameter(configCmd, parameterName, shortParameterName, out string value))
            {
                return value;
            }

            throw new ArgumentNullOrDefaultException(parameterName, $"Argument {parameterName} is not specified.");
        }

        private static bool TryGetValueForParameter(CommandLineApplication configCmd, string parameterName, string? shortParameterName, out string value)
        {
            if (configCmd == null)
            {
                throw new ArgumentNullException(nameof(configCmd));
            }

            if (parameterName == null)
            {
                throw new ArgumentNullException(nameof(parameterName));
            }

            var cmdOptionParameter = configCmd
                .GetOptions()
                .FirstOrDefault(x => x.LongName!.Equals(parameterName, StringComparison.OrdinalIgnoreCase));

            if (cmdOptionParameter == null && shortParameterName != null)
            {
                cmdOptionParameter = configCmd
                    .GetOptions()
                    .FirstOrDefault(x => x.ShortName!.Equals(shortParameterName, StringComparison.OrdinalIgnoreCase));
            }

            if (cmdOptionParameter == null || string.IsNullOrEmpty(cmdOptionParameter.Value()))
            {
                value = string.Empty;
                return false;
            }

            value = cmdOptionParameter.Value()!;
            return true;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace Atc.CodingRules.Updater.CLI
{
    public static class CommandLineApplicationHelper
    {
        public static bool GetHelpMode(CommandLineApplication configCmd)
        {
            return IsParameterDefined(configCmd, "help", "h") ||
                   IsParameterDefined(configCmd, "help", "?");
        }

        public static bool GetVerboseMode(CommandLineApplication configCmd)
        {
            return IsParameterDefined(configCmd, "verboseMode", "v");
        }

        public static DirectoryInfo GetRootPath(CommandLineApplication configCmd)
            => new DirectoryInfo(GetValueForParameter(configCmd, "outputRootPath", "r"));

        public static FileInfo? GetBuildFile(CommandLineApplication configCmd)
        {
            if (TryGetValueForParameter(configCmd, "buildFile", shortParameterName: null, out string value))
            {
                return value.Contains(':', StringComparison.Ordinal)
                    ? new FileInfo(value)
                    : new FileInfo(Path.Combine(GetRootPath(configCmd).FullName, value));
            }

            return null;
        }

        public static bool GetUseTemporarySuppressions(CommandLineApplication configCmd)
        {
            return IsParameterDefined(configCmd, "useTemporarySuppressions", null);
        }

        public static DirectoryInfo? GetTemporarySuppressionsPath(CommandLineApplication configCmd)
        {
            return TryGetValueForParameter(configCmd, "temporarySuppressionPath", null, out string value)
                ? new DirectoryInfo(value)
                : null;
        }

        private static string GetValueForParameter(CommandLineApplication configCmd, string parameterName, string? shortParameterName = null)
        {
            if (TryGetValueForParameter(configCmd, parameterName, shortParameterName, out string value))
            {
                return value;
            }

            throw new ArgumentNullOrDefaultException(parameterName, $"Argument {parameterName} is not specified.");
        }

        private static bool IsParameterDefined(CommandLineApplication configCmd, string parameterName, string? shortParameterName)
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

            return cmdOptionParameter != null && cmdOptionParameter.HasValue();
        }

        private static bool TryGetValueForParameter(
            CommandLineApplication configCmd,
            string parameterName,
            string? shortParameterName,
            out string value)
        {
            if (configCmd is null)
            {
                throw new ArgumentNullException(nameof(configCmd));
            }

            if (parameterName is null)
            {
                throw new ArgumentNullException(nameof(parameterName));
            }

            var cmdOptionParameter = configCmd
                .GetOptions()
                .FirstOrDefault(x => x.LongName!.Equals(parameterName, StringComparison.OrdinalIgnoreCase));

            if (cmdOptionParameter is null && shortParameterName != null)
            {
                cmdOptionParameter = configCmd
                    .GetOptions()
                    .FirstOrDefault(x => x.ShortName!.Equals(shortParameterName, StringComparison.OrdinalIgnoreCase));
            }

            if (cmdOptionParameter is null || string.IsNullOrEmpty(cmdOptionParameter.Value()))
            {
                value = string.Empty;
                return false;
            }

            value = cmdOptionParameter.Value()!;
            return true;
        }
    }
}
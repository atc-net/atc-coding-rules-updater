using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Atc.CodingRules.Updater.CLI.Commands.Settings
{
    public class RootCommandSettings : CommandSettings
    {
        [CommandOption("-r|--outputRootPath <OUTPUTROOTPATH>")]
        [Description("Path to the root directory")]
        public string OutputRootPath { get; init; } = string.Empty;

        [CommandOption("-o|--optionsPath [OPTIONSPATH]")]
        [Description("Path to an optional options json-file")]
        public FlagValue<string>? OptionsPath { get; init; }

        [CommandOption("--useTemporarySuppressions [USETEMPORARYSUPPRESSIONS]")]
        [Description("Indicate if build process should use temporary suppressions - appends to .editorconfig - unless temporarySuppressionPath is set")]
        public FlagValue<bool>? UseTemporarySuppressions { get; init; }

        [CommandOption("--temporarySuppressionPath [TEMPORARYSUPPRESSIONPATH]")]
        [Description("Optional path to temporary suppressions file - if not set .editorconfig file is used")]
        public FlagValue<string>? TemporarySuppressionsPath { get; init; }

        [CommandOption("--temporarySuppressionAsExcel [TEMPORARYSUPPRESSIONASEXCEL]")]
        [Description("Indicate if temporary suppressions file should be saved as Excel (.xlsx)")]
        public FlagValue<bool>? TemporarySuppressionAsExcel { get; init; }

        [CommandOption("--buildFile [BUILDFILE]")]
        [Description("Solution/project file - required when multiple .sln files exists in root path")]
        public FlagValue<string>? BuildFile { get; init; }

        [CommandOption("-v|--verboseMode [VERBOSEMODE]")]
        [Description("Use verboseMode for more debug/trace information")]
        public FlagValue<string>? VerboseMode { get; init; }

        public override ValidationResult Validate()
        {
            var validationResult = base.Validate();
            if (!validationResult.Successful)
            {
                return validationResult;
            }

            return string.IsNullOrEmpty(OutputRootPath)
                ? ValidationResult.Error("OutputRootPath is missing.")
                : ValidationResult.Success();
        }
    }
}
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace Atc.CodingRules.Updater.CLI.Commands.CommandOptions
{
    public class BaseCommandOptions
    {
        [Option("--verboseMode", "Use verboseMode for more debug/trace information", CommandOptionType.NoValue, ShortName = "v")]
        public bool VerboseMode { get; set; }

        [Required]
        [Option("--outputRootPath", "Path to root directory", CommandOptionType.SingleValue, ShortName = "r")]
        public string? OutputRootPath { get; set; }

        [Option("--optionsPath", "Path to options json-file", CommandOptionType.SingleValue)]
        public string? OptionsPath { get; set; }

        [Option("--useTemporarySuppressions", "Use temporary suppressions from build - appends to .editorconfig - unless temporarySuppressionPath is set", CommandOptionType.NoValue)]
        public bool UseTemporarySuppressions { get; set; }

        [Option("--temporarySuppressionPath", "Optional path to temporary suppressions file - if not set .editorconfig file is used", CommandOptionType.SingleValue)]
        public string? TemporarySuppressionsPath { get; set; }

        [Option("--temporarySuppressionAsExcel", "Optional - save temporary suppressions file as Excel (.xlsx)", CommandOptionType.NoValue)]
        public bool TemporarySuppressionAsExcel { get; set; }

        [Option("--buildFile", "Optional path to solution/project file - required when multiple .sln files exists in root path", CommandOptionType.SingleValue)]
        public string? BuildFile { get; set; }
    }
}
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

        [Option("--useTemporarySuppressions", "???", CommandOptionType.NoValue)]
        public bool UseTemporarySuppressions { get; set; }

        [Option("--temporarySuppressionPath", "???", CommandOptionType.SingleValue)]
        public string? TemporarySuppressionsPath { get; set; }
    }
}
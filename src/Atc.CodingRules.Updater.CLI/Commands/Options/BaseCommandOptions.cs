using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace Atc.CodingRules.Updater.CLI.Commands.Options
{
    public class BaseCommandOptions
    {
        [Option("--verboseMode", "Use verboseMode for more debug/trace information", CommandOptionType.SingleValue, ShortName = "v")]
        public string? VerboseMode { get; set; }

        [Required]
        [Option("--outputRootPath", "Path to root directory", CommandOptionType.SingleValue, ShortName = "r")]
        public string? OutputRootPath { get; set; }
    }
}

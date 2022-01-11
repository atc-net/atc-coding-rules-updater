using System.ComponentModel;

namespace Atc.CodingRules.Updater.CLI.Commands.Settings
{
    public class BaseCommandSettings : CommandSettings
    {
        [CommandOption("-r|--outputRootPath <OUTPUTROOTPATH>")]
        [Description("Path to the root directory (default current diectory)")]
        public string OutputRootPath { get; init; } = string.Empty;

        [CommandOption("-v|--verbose")]
        [Description("Use verbose for more debug/trace information")]
        public bool Verbose { get; init; }

        [CommandOption("-o|--optionsPath [OPTIONSPATH]")]
        [Description("Path to an optional options json-file")]
        public FlagValue<string>? OptionsPath { get; init; }

        internal string GetOptionsPath()
        {
            var optionsPath = string.Empty;
            if (this.OptionsPath is not null && this.OptionsPath.IsSet)
            {
                optionsPath = this.OptionsPath.Value;
            }

            return optionsPath;
        }
    }
}
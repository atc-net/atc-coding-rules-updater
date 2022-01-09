using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class RootCommandSettings : CommandSettings
{
    [CommandOption("-r|--outputRootPath <OUTPUTROOTPATH>")]
    [Description("Path to the root directory (default current diectory)")]
    public string OutputRootPath { get; init; } = string.Empty;

    [CommandOption("-t|--solutionTarget [SOLUTIONTARGET]")]
    [Description("Solution target: dotnet5, dotnet6 (default dotnet6)")]
    public FlagValue<string>? SolutionTarget { get; init; }

    [CommandOption("-o|--optionsPath [OPTIONSPATH]")]
    [Description("Path to an optional options json-file")]
    public FlagValue<string>? OptionsPath { get; init; }

    [CommandOption("--useLatestMinorNugetVersion")]
    [Description("Indicate if nuget packages should by updated to latest minor version (default true)")]
    public bool? UseLatestMinorNugetVersion { get; init; }

    [CommandOption("--useTemporarySuppressions")]
    [Description("Indicate if build process should use temporary suppressions - appends to .editorconfig - unless temporarySuppressionPath is set")]
    public bool? UseTemporarySuppressions { get; init; }

    [CommandOption("--temporarySuppressionPath [TEMPORARYSUPPRESSIONPATH]")]
    [Description("Optional path to temporary suppressions file - if not set .editorconfig file is used")]
    public FlagValue<string>? TemporarySuppressionsPath { get; init; }

    [CommandOption("--temporarySuppressionAsExcel")]
    [Description("Indicate if temporary suppressions file should be saved as Excel (.xlsx)")]
    public bool? TemporarySuppressionAsExcel { get; init; }

    [CommandOption("--buildFile [BUILDFILE]")]
    [Description("Solution/project file - required when multiple .sln files exists in root path")]
    public FlagValue<string>? BuildFile { get; init; }

    [CommandOption("-v|--verboseMode")]
    [Description("Use verboseMode for more debug/trace information")]
    public bool VerboseMode { get; init; }

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
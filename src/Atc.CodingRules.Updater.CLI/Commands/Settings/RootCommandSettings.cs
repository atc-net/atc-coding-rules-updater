using System.ComponentModel;
using Spectre.Console;

namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class RootCommandSettings : BaseCommandSettings
{
    [CommandOption("-t|--solutionTarget [SOLUTIONTARGET]")]
    [Description("Solution target: dotnet5, dotnet6 (default dotnet6)")]
    public FlagValue<string>? SolutionTarget { get; init; }

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
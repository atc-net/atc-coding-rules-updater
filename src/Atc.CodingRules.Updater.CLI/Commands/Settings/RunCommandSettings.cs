// ReSharper disable StringLiteralTypo
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class RunCommandSettings : ProjectCommandSettings
{
    [CommandOption(ArgumentCommandConstants.LongUseLatestMinorNugetVersion)]
    [Description("Indicate if nuget packages should by updated to latest minor version (default true)")]
    public bool? UseLatestMinorNugetVersion { get; init; }

    [CommandOption($"{ArgumentCommandConstants.ShortUseTemporarySuppressions}|{ArgumentCommandConstants.LongUseTemporarySuppressions}")]
    [Description("Indicate if build process should use temporary suppressions - appends to .editorconfig - unless temporarySuppressionPath is set")]
    public bool? UseTemporarySuppressions { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongTemporarySuppressionPath} [TEMPORARYSUPPRESSIONPATH]")]
    [Description("Optional path to temporary suppressions file - if not set .editorconfig file is used")]
    public FlagValue<string>? TemporarySuppressionsPath { get; init; }

    [CommandOption(ArgumentCommandConstants.LongTemporarySuppressionAsExcel)]
    [Description("Indicate if temporary suppressions file should be saved as Excel (.xlsx)")]
    public bool? TemporarySuppressionAsExcel { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongBuildFile} [BUILDFILE]")]
    [Description("Solution/project file - required when multiple .sln files exists in root path")]
    public FlagValue<string>? BuildFile { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongOrganizationName} [ORGANIZATIONNAME]")]
    [Description("Optional: Specify the name of your organization for the Directory.Build.Props file")]
    public FlagValue<string>? OrganizationName { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongRepositoryName} [REPOSITORYNAME]")]
    [Description("Optional: Specify the name of your repository for the Directory.Build.Props file")]
    public FlagValue<string>? RepositoryName { get; init; }
}
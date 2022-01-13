using System.ComponentModel;

// ReSharper disable StringLiteralTypo
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class RunCommandSettings : ProjectCommandSettings
{
    [CommandOption(CommandConstants.ArgumentLongUseLatestMinorNugetVersion)]
    [Description("Indicate if nuget packages should by updated to latest minor version (default true)")]
    public bool? UseLatestMinorNugetVersion { get; init; }

    [CommandOption(CommandConstants.ArgumentLongUseTemporarySuppressions)]
    [Description("Indicate if build process should use temporary suppressions - appends to .editorconfig - unless temporarySuppressionPath is set")]
    public bool? UseTemporarySuppressions { get; init; }

    [CommandOption($"{CommandConstants.ArgumentLongTemporarySuppressionPath} [TEMPORARYSUPPRESSIONPATH]")]
    [Description("Optional path to temporary suppressions file - if not set .editorconfig file is used")]
    public FlagValue<string>? TemporarySuppressionsPath { get; init; }

    [CommandOption(CommandConstants.ArgumentLongTemporarySuppressionAsExcel)]
    [Description("Indicate if temporary suppressions file should be saved as Excel (.xlsx)")]
    public bool? TemporarySuppressionAsExcel { get; init; }

    [CommandOption($"{CommandConstants.ArgumentLongBuildFile} [BUILDFILE]")]
    [Description("Solution/project file - required when multiple .sln files exists in root path")]
    public FlagValue<string>? BuildFile { get; init; }

    [CommandOption($"{CommandConstants.ArgumentLongOrganizationName} [ORGANIZATIONNAME]")]
    [Description("Optional: Specify the name of your organization for the Directory.Build.Props file")]
    public FlagValue<string>? OrganizationName { get; init; }

    [CommandOption($"{CommandConstants.ArgumentLongRepositoryName} [REPOSITORYNAME]")]
    [Description("Optional: Specify the name of your repository for the Directory.Build.Props file")]
    public FlagValue<string>? RepositoryName { get; init; }
}
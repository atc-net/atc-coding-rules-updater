// ReSharper disable StringLiteralTypo
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class RunCommandSettings : ProjectCommandSettings
{
    [CommandOption(ArgumentCommandConstants.LongUseLatestMinorNugetVersion)]
    [Description("Bump PackageReferences in Directory.Build.props to the latest available minor version. (default true)")]
    public bool? UseLatestMinorNugetVersion { get; init; }

    [CommandOption($"{ArgumentCommandConstants.ShortUseTemporarySuppressions}|{ArgumentCommandConstants.LongUseTemporarySuppressions}")]
    [Description("Build the project, collect analyzer errors, and write temporary suppressions. Appended to .editorconfig unless --temporarySuppressionPath is set. (default false)")]
    public bool? UseTemporarySuppressions { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongTemporarySuppressionPath} [TEMPORARYSUPPRESSIONPATH]")]
    [Description("Optional output directory for the temporary-suppressions file. When set, the .editorconfig is not modified.")]
    public FlagValue<string>? TemporarySuppressionsPath { get; init; }

    [CommandOption(ArgumentCommandConstants.LongTemporarySuppressionAsExcel)]
    [Description("Write the temporary-suppressions file as Excel (.xlsx) instead of plain text. (default false)")]
    public bool? TemporarySuppressionAsExcel { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongBuildFile} [BUILDFILE]")]
    [Description("Solution (.sln) or project (.csproj) file to build. Required when multiple .sln files exist in --projectPath.")]
    public FlagValue<string>? BuildFile { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongOrganizationName} [ORGANIZATIONNAME]")]
    [Description("Organization name to substitute into the <OrganizationName> placeholder in Directory.Build.props.")]
    public FlagValue<string>? OrganizationName { get; init; }

    [CommandOption($"{ArgumentCommandConstants.LongRepositoryName} [REPOSITORYNAME]")]
    [Description("Repository name to substitute into the <RepositoryName> placeholder in Directory.Build.props.")]
    public FlagValue<string>? RepositoryName { get; init; }

    [CommandOption(ArgumentCommandConstants.LongDryRun)]
    [Description("Preview mode: log what would be created or updated without writing any files. Skips the temporary-suppression build loop. (default false)")]
    public bool? DryRun { get; init; }
}
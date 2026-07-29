// ReSharper disable StringLiteralTypo
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class RunCommandSettings : ProjectCommandSettings
{
    /// <remarks>
    /// The name is historical: the bump is bounded by the <em>major</em> version, not the minor.
    /// A package on 3.0.54 moves to the newest 3.x, but never to 4.0.0 — see the
    /// <c>withinMinorReleaseOnly</c> argument in <c>DirectoryBuildPropsHelper</c>.
    /// </remarks>
    [CommandOption(ArgumentCommandConstants.LongUseLatestMinorNugetVersion)]
    [Description("Bump PackageReferences in Directory.Build.props to the latest version within the same major version. (default true)")]
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

    [CommandOption(ArgumentCommandConstants.LongFailOnChanges)]
    [Description("Exit with a non-zero code when any file was created or updated. Combine with --dry-run to gate CI on \"coding rules are current\" without writing anything. (default false)")]
    public bool? FailOnChanges { get; init; }

    [CommandOption(ArgumentCommandConstants.LongOutputJson)]
    [Description("Emit a machine-readable JSON summary on stdout (per-file outcomes, package bumps, props drift) instead of log output. Useful for CI.")]
    public bool? OutputJson { get; init; }

    [CommandOption(ArgumentCommandConstants.LongForceNugetRefresh)]
    [Description("Ask the ATC API to re-read package versions from nuget.org instead of serving its 12-hour cache. Use when a just-published version is not being picked up. (default false)")]
    public bool? ForceNugetRefresh { get; init; }
}
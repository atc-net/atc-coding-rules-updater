namespace Atc.CodingRules.Updater.CLI.Commands;

/// <summary>
/// Option names. Long names are kebab-case; the camelCase names released before the switch keep
/// working through <see cref="Program.LegacyOptionNames"/>.
/// </summary>
internal static class ArgumentCommandConstants
{
    public const string ShortProjectPath = "-p";
    public const string LongProjectPath = "--project-path";
    public const string ShortOptionsPath = "-o";
    public const string LongOptionsTarget = "--options-path";
    public const string ShortProjectTarget = "-t";
    public const string LongProjectTarget = "--project-target";
    public const string LongUseLatestMinorNugetVersion = "--use-latest-minor-nuget-version";
    public const string ShortUseTemporarySuppressions = "-s";
    public const string LongUseTemporarySuppressions = "--use-temporary-suppressions";
    public const string LongTemporarySuppressionPath = "--temporary-suppression-path";
    public const string LongTemporarySuppressionAsExcel = "--temporary-suppression-as-excel";
    public const string LongBuildFile = "--build-file";
    public const string LongBuildConfiguration = "--build-configuration";
    public const string LongBuildProperty = "--build-property";
    public const string LongFetchMode = "--fetch-mode";
    public const string LongOrganizationName = "--organization-name";
    public const string LongRepositoryName = "--repository-name";
    public const string LongDryRun = "--dry-run";
    public const string LongForceNugetRefresh = "--force-nuget-refresh";
    public const string LongFailOnChanges = "--fail-on-changes";
    public const string LongIncludeProviders = "--include-providers";
    public const string LongExcludeProviders = "--exclude-providers";
    public const string LongOutputJson = "--json";
    public const string LongCentralPackageManagement = "--central-package-management";
    public const string LongUnifyVersions = "--unify-versions";
    public const string LongSkipRestoreVerification = "--skip-restore-verification";
    public const string LongAllowDirty = "--allow-dirty";
}
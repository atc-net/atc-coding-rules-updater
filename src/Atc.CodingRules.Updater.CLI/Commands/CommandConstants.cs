namespace Atc.CodingRules.Updater.CLI.Commands;

internal static class CommandConstants
{
    public const string ArgumentShortHelp = "-h";
    public const string ArgumentLongHelp = "--help";
    public const string ArgumentLongVersion = "--version";
    public const string ArgumentShortVerbose = "-v";
    public const string ArgumentLongVerbose = "--verbose";
    public const string ArgumentShortProjectPath = "-p";
    public const string ArgumentLongProjectPath = "--projectPath";
    public const string ArgumentShortOptionsPath = "-o";
    public const string ArgumentLongOptionsTarget = "--optionsPath";
    public const string ArgumentShortProjectTarget = "-t";
    public const string ArgumentLongProjectTarget = "--projectTarget";
    public const string ArgumentLongUseLatestMinorNugetVersion = "--useLatestMinorNugetVersion";
    public const string ArgumentLongUseTemporarySuppressions = "--useTemporarySuppressions";
    public const string ArgumentLongTemporarySuppressionPath = "--temporarySuppressionPath";
    public const string ArgumentLongTemporarySuppressionAsExcel = "--temporarySuppressionAsExcel";
    public const string ArgumentLongBuildFile = "--buildFile";
    public const string ArgumentLongFetchMode = "--fetchMode";
    public const string ArgumentLongOrganizationName = " --organizationName";
    public const string ArgumentLongRepositoryName = " --repositoryName";

    public const string NameSanityCheck = "sanity-check";

    public const string NameOptionsFile = "options-file";
    public const string NameOptionsFileCreate = "create";
    public const string NameOptionsFileValidate = "validate";

    public const string NameAnalyzerProviders = "analyzer-providers";
    public const string NameAnalyzerProvidersCollect = "collect";
    public const string NameAnalyzerProvidersCleanupCache = "cleanup-cache";

    public const string NameRun = "run";
}
// ReSharper disable StringLiteralTypo
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class AnalyzerProvidersCollectCommandSettings : ProjectBaseCommandSettings
{
    [CommandOption($"{ArgumentCommandConstants.LongFetchMode} [FETCHMODE]")]
    [ProviderCollectingModeDescription]
    public FlagValue<ProviderCollectingMode> FetchMode { get; init; } = new FlagValue<ProviderCollectingMode>();

    [CommandOption($"{ArgumentCommandConstants.LongIncludeProviders} [INCLUDEPROVIDERS]")]
    [Description("Comma-separated provider names to include (e.g. \"AsyncFixer,Meziantou.Analyzer\"). When set, only these providers run.")]
    public FlagValue<string> IncludeProviders { get; init; } = new FlagValue<string>();

    [CommandOption($"{ArgumentCommandConstants.LongExcludeProviders} [EXCLUDEPROVIDERS]")]
    [Description("Comma-separated provider names to exclude (e.g. \"SonarAnalyzer.CSharp\"). Applied after --includeProviders.")]
    public FlagValue<string> ExcludeProviders { get; init; } = new FlagValue<string>();

    [CommandOption(ArgumentCommandConstants.LongOutputJson)]
    [Description("Emit a machine-readable JSON summary on stdout (Name / Rules.Count / ExceptionMessage per provider). Useful for CI.")]
    public bool? OutputJson { get; init; }
}
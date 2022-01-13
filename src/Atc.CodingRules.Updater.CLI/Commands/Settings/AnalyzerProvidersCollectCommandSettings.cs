// ReSharper disable StringLiteralTypo
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class AnalyzerProvidersCollectCommandSettings : ProjectBaseCommandSettings
{
    [CommandOption($"{CommandConstants.ArgumentLongFetchMode} [FETCHMODE]")]
    [ProviderCollectingModeDescription]
    public FlagValue<ProviderCollectingMode> FetchMode { get; init; } = new FlagValue<ProviderCollectingMode>();
}
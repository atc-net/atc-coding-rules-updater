namespace Atc.CodingRules.Updater.CLI.Commands.Settings
{
    public class AnalyzerProvidersCollectCommandSettings : BaseCommandSettings
    {
        [CommandOption($"{CommandConstants.ArgumentLongFetchMode} [FETCHMODE]")]
        [ProviderCollectingModeDescription]
        public FlagValue<ProviderCollectingMode> FetchMode { get; init; } = new FlagValue<ProviderCollectingMode>();
    }
}
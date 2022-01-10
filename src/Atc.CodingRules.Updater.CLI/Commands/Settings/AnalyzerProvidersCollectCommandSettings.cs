namespace Atc.CodingRules.Updater.CLI.Commands.Settings
{
    public class AnalyzerProvidersCollectCommandSettings : BaseCommandSettings
    {
        [CommandOption("--collectingMode [COLLECTINGMODE]")]
        [ProviderCollectingModeDescription]
        public FlagValue<ProviderCollectingMode> CollectingMode { get; init; } = new FlagValue<ProviderCollectingMode>();
    }
}
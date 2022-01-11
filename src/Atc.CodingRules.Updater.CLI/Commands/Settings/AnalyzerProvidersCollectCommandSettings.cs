namespace Atc.CodingRules.Updater.CLI.Commands.Settings
{
    public class AnalyzerProvidersCollectCommandSettings : BaseCommandSettings
    {
        [CommandOption("--updateRules [UPDATERULES]")]
        [ProviderCollectingModeDescription]
        public FlagValue<ProviderCollectingMode> UpdateRules { get; init; } = new FlagValue<ProviderCollectingMode>();
    }
}
namespace Atc.CodingRules.Updater.CLI.Commands.Settings
{
    public class AnalyzerProvidersCollectCommandSettings : BaseCommandSettings
    {
        [CommandOption("--fetchMode [FETCHMODE]")]
        [ProviderCollectingModeDescription]
        public FlagValue<ProviderCollectingMode> FetchMode { get; init; } = new FlagValue<ProviderCollectingMode>();
    }
}
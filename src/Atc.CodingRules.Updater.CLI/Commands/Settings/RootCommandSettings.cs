using System.ComponentModel;

namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class RootCommandSettings : CommandSettings
{
    [CommandOption(CommandConstants.ArgumentLongVersion)]
    [Description("Display version")]
    public bool? Version { get; init; }
}
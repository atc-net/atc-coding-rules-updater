using Spectre.Console;

namespace Atc.CodingRules.Updater.CLI.Commands;

public class RootCommand : Command<RootCommandSettings>
{
    private readonly ILogger<RootCommand> logger;

    public RootCommand(ILogger<RootCommand> logger) => this.logger = logger;

    public override int Execute(
        CommandContext context,
        RootCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        if (IsOptionValueTrue(settings.Version))
        {
            HandleVersionOption();
        }

        return ConsoleExitStatusCodes.Success;
    }

    private static bool IsOptionValueTrue(bool? value)
        => value is not null && value.Value;

    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "OK.")]
    private static void HandleVersionOption()
    {
        System.Console.WriteLine(CodingRulesUpdaterVersionHelper.GetCurrentVersion().ToString());
        if (CodingRulesUpdaterVersionHelper.IsLatestVersion())
        {
            return;
        }

        var latestVersion = CodingRulesUpdaterVersionHelper.GetLatestVersion()!;
        System.Console.WriteLine(string.Empty);
        System.Console.WriteLine($"Version {latestVersion} of ATC-Coding-Rules-Updater is available!");
        System.Console.WriteLine(string.Empty);
        System.Console.WriteLine("To update run the following command:");
        System.Console.WriteLine("   dotnet tool update --global atc-coding-rules-updater");
    }
}
namespace Atc.CodingRules.Updater.CLI.Commands;

public class RootCommand : AsyncCommand<RootCommandSettings>
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        RootCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private static async Task<int> ExecuteInternalAsync(
        RootCommandSettings settings)
    {
        if (settings.IsOptionValueTrue(settings.Version))
        {
            try
            {
                HandleVersionOption();
            }
            catch
            {
                return ConsoleExitStatusCodes.Failure;
            }
        }

        await Task.Delay(1);
        return ConsoleExitStatusCodes.Success;
    }

    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "OK.")]
    private static void HandleVersionOption()
    {
        System.Console.WriteLine(CliHelper.GetCurrentVersion().ToString());
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
// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI.Commands
{
    public class VersionCommand : Command
    {
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "OK.")]
        public override int Execute(CommandContext context)
        {
            System.Console.WriteLine(CodingRulesUpdaterVersionHelper.GetCurrentVersion().ToString());
            if (!CodingRulesUpdaterVersionHelper.IsLatestVersion())
            {
                var latestVersion = CodingRulesUpdaterVersionHelper.GetLatestVersion()!;
                System.Console.WriteLine(string.Empty);
                System.Console.WriteLine($"Version {latestVersion} of ATC-Coding-Rules-Updater is available!");
                System.Console.WriteLine(string.Empty);
                System.Console.WriteLine("To update run the following command:");
                System.Console.WriteLine("   dotnet tool update --global atc-coding-rules-updater");
            }

            return ConsoleExitStatusCodes.Success;
        }
    }
}
namespace Atc.CodingRules.Updater.CLI;

public static class ConsoleHelper
{
    public static void WriteHeader()
        => Console.Spectre.Helpers.ConsoleHelper.WriteHeader("Rules updater");
}
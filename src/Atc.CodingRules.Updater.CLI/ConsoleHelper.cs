using Spectre.Console;

namespace Atc.CodingRules.Updater.CLI;

public static class ConsoleHelper
{
    public static void WriteHeader()
        => AnsiConsole.Write(new FigletText("Rules updater").Color(Color.CornflowerBlue));
}
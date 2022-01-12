using Atc.CodingRules.Updater.CLI.Extensions;

[assembly: CLSCompliant(false)]

namespace Atc.CodingRules.Updater.CLI;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static Task<int> Main(string[] args)
    {
        args = SetHelpArgumentIfNeeded(args);
        args = SetProjectPathArgumentIfNeeded(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration.GetRequiredSection("ConsoleLogger").Bind(consoleLoggerConfiguration);

        SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

        var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);

        var app = CommandAppFactory.CreateWithRootCommand<RootCommand>(serviceCollection);
        app.ConfigureCommands();

        return app.RunAsync(args);
    }

    private static string[] SetHelpArgumentIfNeeded(string[] args)
    {
        switch (args.Length)
        {
            case 0:
                return new[] { "-h", };
            case 2 when args[0].Equals("option-file", StringComparison.OrdinalIgnoreCase) &&
                        (args[1].Equals("create", StringComparison.OrdinalIgnoreCase) ||
                         args[1].Equals("validate", StringComparison.OrdinalIgnoreCase)):
                Array.Resize(ref args, args.Length + 1);
                args[^1] = "-h";
                return args;
            default:
                return args;
        }
    }

    private static string[] SetProjectPathArgumentIfNeeded(string[] args)
    {
        var dot = args.FirstOrDefault(x => x.Equals(".", StringComparison.Ordinal));
        if (string.IsNullOrEmpty(dot))
        {
            if (args.Length == 1 &&
                (args[0].Equals("version", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("--version", StringComparison.OrdinalIgnoreCase)))
            {
                // Since -p|--projectPath is required from RootCommand,
                // a default projectPath is appended to args
                return new[] { "version", "-p", Environment.CurrentDirectory };
            }

            return args;
        }

        if (args.Length == 1)
        {
            // Change "." => "-p [CurrentDirectory]"
            return new[] { "-p", Environment.CurrentDirectory };
        }

        // Replace "." with [CurrentDirectory]
        return args
            .Select(x => x.Equals(".", StringComparison.Ordinal)
                ? Environment.CurrentDirectory
                : x)
            .ToArray();
    }

    private static void SetMinimumLogLevelIfNeeded(
        string[] args,
        ConsoleLoggerConfiguration consoleLoggerConfiguration)
    {
        if (args.Any(x => x.Equals("-v", StringComparison.OrdinalIgnoreCase)) ||
            args.Any(x => x.Equals("--verbose", StringComparison.OrdinalIgnoreCase)))
        {
            consoleLoggerConfiguration.MinimumLogLevel = LogLevel.Trace;
        }
    }
}
[assembly: CLSCompliant(false)]

namespace Atc.CodingRules.Updater.CLI;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static Task<int> Main(string[] args)
    {
        args = SetHelpArgumentIfNeeded(args);
        args = SetOutputRootPathArgumentIfNeeded(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration.GetRequiredSection("ConsoleLogger").Bind(consoleLoggerConfiguration);

        SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

        var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);

        var app = CommandAppFactory.CreateWithSingleCommand<RootCommand>(serviceCollection);

        return app.RunAsync(args);
    }

    private static string[] SetHelpArgumentIfNeeded(string[] args)
        => args.Any()
            ? args
            : new[] { "-h", };

    private static string[] SetOutputRootPathArgumentIfNeeded(string[] args)
    {
        var dot = args.FirstOrDefault(x => x.Equals(".", StringComparison.Ordinal));
        if (string.IsNullOrEmpty(dot))
        {
            return args;
        }

        if (args.Length == 1)
        {
            return new[] { "-r", Environment.CurrentDirectory };
        }

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
            args.Any(x => x.Equals("--verboseMode", StringComparison.OrdinalIgnoreCase)))
        {
            consoleLoggerConfiguration.MinimumLogLevel = LogLevel.Trace;
        }
    }
}
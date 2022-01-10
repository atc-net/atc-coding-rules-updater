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

        var app = CommandAppFactory.CreateWithRootCommand<RootCommand>(serviceCollection);
        app.Configure(config =>
        {
            config.AddBranch("options-file", options =>
            {
                options.AddCommand<OptionsFileCreateCommand>("create");
                options.AddCommand<OptionsFileValidateCommand>("validate");
            });

            config.AddBranch("analyzer-providers", optionsFile =>
            {
                optionsFile.AddCommand<AnalyzerProvidersCollectCommand>("collect");
                optionsFile.AddCommand<AnalyzerProvidersCacheCleanupCommand>("cache-cleanup");
            });
        });

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
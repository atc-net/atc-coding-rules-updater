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
        app.Configure(config =>
        {
            config.AddBranch("options-file", options =>
            {
                options
                    .AddCommand<OptionsFileCreateCommand>("create")
                    .WithDescription("Create default options file (atc-coding-rules-updater.json) if it doesn't exist");
                options
                    .AddCommand<OptionsFileValidateCommand>("validate")
                    .WithDescription("Validate the options file (atc-coding-rules-updater.json)");
            });

            config.AddCommand<SanityCheckCommand>("sanity-check")
                .WithDescription("Sanity check the project files.");

            config.AddBranch("analyzer-providers", optionsFile =>
            {
                optionsFile
                    .AddCommand<AnalyzerProvidersCollectCommand>("collect")
                    .WithDescription("Collect base rules metadata from all Analyzer providers");
                optionsFile
                    .AddCommand<AnalyzerProvidersCacheCleanupCommand>("cache-cleanup")
                    .WithDescription("Cleanup cache from Analyzer providers");
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

    private static string[] SetProjectPathArgumentIfNeeded(string[] args)
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
            args.Any(x => x.Equals("--verbose", StringComparison.OrdinalIgnoreCase)))
        {
            consoleLoggerConfiguration.MinimumLogLevel = LogLevel.Trace;
        }
    }
}
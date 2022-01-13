using Atc.CodingRules.Updater.CLI.Extensions;

[assembly: CLSCompliant(false)]

// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static Task<int> Main(string[] args)
    {
        args = SetProjectPathFromDotArgumentIfNeeded(args);
        args = SetHelpArgumentIfNeeded(args);

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

    private static string[] SetProjectPathFromDotArgumentIfNeeded(string[] args)
    {
        if (args.Any())
        {
            return args;
        }

        if (!args.Contains("."))
        {
            return args;
        }

        var newArgs = new List<string>();
        foreach (var s in args)
        {
            if (".".Equals(s, StringComparison.Ordinal))
            {
                if (!(newArgs.Contains(CommandConstants.ArgumentShortProjectPath) ||
                      newArgs.Contains(CommandConstants.ArgumentLongProjectPath)))
                {
                    newArgs.Add(CommandConstants.ArgumentShortProjectPath);
                }

                newArgs.Add(Environment.CurrentDirectory);
            }
            else
            {
                newArgs.Add(s);
            }
        }

        return newArgs.ToArray();
    }

    private static string[] SetHelpArgumentIfNeeded(string[] args)
    {
        if (args.Length == 0)
        {
            return new[] { CommandConstants.ArgumentShortHelp, };
        }

        if (!(args.Contains(CommandConstants.ArgumentShortProjectPath) ||
              args.Contains(CommandConstants.ArgumentLongProjectPath)))
        {
            if (args.Contains(CommandConstants.NameSanityCheck))
            {
                return new[] { CommandConstants.NameSanityCheck, CommandConstants.ArgumentShortHelp, };
            }

            if (args.Contains(CommandConstants.NameOptionsFile) &&
                (args.Contains(CommandConstants.NameOptionsFileCreate) ||
                 args.Contains(CommandConstants.NameOptionsFileValidate)))
            {
                return new[] { CommandConstants.NameOptionsFile, CommandConstants.ArgumentShortHelp, };
            }

            if (args.Contains(CommandConstants.NameAnalyzerProviders) &&
                (args.Contains(CommandConstants.NameAnalyzerProvidersCollect) ||
                 args.Contains(CommandConstants.NameAnalyzerProvidersCleanupCache)))
            {
                return new[] { CommandConstants.NameAnalyzerProviders, CommandConstants.ArgumentShortHelp, };
            }

            if (args.Contains(CommandConstants.NameRun))
            {
                return new[] { CommandConstants.NameRun, CommandConstants.ArgumentShortHelp, };
            }
        }

        return args;
    }

    private static void SetMinimumLogLevelIfNeeded(
        string[] args,
        ConsoleLoggerConfiguration consoleLoggerConfiguration)
    {
        if (args.Any(x => x.Equals(CommandConstants.ArgumentShortVerbose, StringComparison.OrdinalIgnoreCase)) ||
            args.Any(x => x.Equals(CommandConstants.ArgumentLongVerbose, StringComparison.OrdinalIgnoreCase)))
        {
            consoleLoggerConfiguration.MinimumLogLevel = LogLevel.Trace;
        }
    }
}
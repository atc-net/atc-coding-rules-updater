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
        if (!args.Contains(".", StringComparer.Ordinal))
        {
            return args;
        }

        var newArgs = new List<string>();
        foreach (var s in args)
        {
            if (".".Equals(s, StringComparison.Ordinal))
            {
                if (!(newArgs.Contains(CommandConstants.ArgumentShortProjectPath, StringComparer.OrdinalIgnoreCase) ||
                      newArgs.Contains(CommandConstants.ArgumentLongProjectPath, StringComparer.OrdinalIgnoreCase)))
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

        if (!newArgs.Contains(CommandConstants.NameRun, StringComparer.OrdinalIgnoreCase) &&
            !newArgs.Contains(CommandConstants.NameSanityCheck, StringComparer.OrdinalIgnoreCase) &&
            !newArgs.Contains(CommandConstants.NameOptionsFile, StringComparer.OrdinalIgnoreCase) &&
            !newArgs.Contains(CommandConstants.NameAnalyzerProviders, StringComparer.OrdinalIgnoreCase) &&
            (newArgs.Contains(CommandConstants.ArgumentShortProjectPath, StringComparer.OrdinalIgnoreCase) ||
             newArgs.Contains(CommandConstants.ArgumentLongProjectPath, StringComparer.OrdinalIgnoreCase)))
        {
            newArgs.Insert(0, CommandConstants.NameRun);
        }

        return newArgs.ToArray();
    }

    private static string[] SetHelpArgumentIfNeeded(string[] args)
    {
        if (args.Length == 0)
        {
            return new[] { CommandConstants.ArgumentShortHelp, };
        }

        if (args.Contains(CommandConstants.NameAnalyzerProviders, StringComparer.OrdinalIgnoreCase) &&
            args.Contains(CommandConstants.NameAnalyzerProvidersCleanupCache, StringComparer.OrdinalIgnoreCase))
        {
            return args;
        }

        if (!(args.Contains(CommandConstants.ArgumentShortProjectPath, StringComparer.OrdinalIgnoreCase) ||
              args.Contains(CommandConstants.ArgumentLongProjectPath, StringComparer.OrdinalIgnoreCase)))
        {
            if (args.Contains(CommandConstants.NameSanityCheck, StringComparer.OrdinalIgnoreCase))
            {
                return new[] { CommandConstants.NameSanityCheck, CommandConstants.ArgumentShortHelp, };
            }

            if (args.Contains(CommandConstants.NameOptionsFile, StringComparer.OrdinalIgnoreCase) &&
                (args.Contains(CommandConstants.NameOptionsFileCreate, StringComparer.OrdinalIgnoreCase) ||
                 args.Contains(CommandConstants.NameOptionsFileValidate, StringComparer.OrdinalIgnoreCase)))
            {
                return new[] { CommandConstants.NameOptionsFile, CommandConstants.ArgumentShortHelp, };
            }

            if (args.Contains(CommandConstants.NameAnalyzerProviders, StringComparer.OrdinalIgnoreCase) &&
                args.Contains(CommandConstants.NameAnalyzerProvidersCollect, StringComparer.OrdinalIgnoreCase))
            {
                return new[] { CommandConstants.NameAnalyzerProviders, CommandConstants.ArgumentShortHelp, };
            }

            if (args.Contains(CommandConstants.NameRun, StringComparer.OrdinalIgnoreCase))
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
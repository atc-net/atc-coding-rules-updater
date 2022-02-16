[assembly: CLSCompliant(false)]

// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        args = SetProjectPathFromDotArgumentIfNeeded(args);
        args = SetHelpArgumentIfNeeded(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration.GetRequiredSection("ConsoleLogger").Bind(consoleLoggerConfiguration);

        ProgramCsHelper.SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

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
                if (!(newArgs.Contains(ArgumentCommandConstants.ShortProjectPath, StringComparer.OrdinalIgnoreCase) ||
                      newArgs.Contains(ArgumentCommandConstants.LongProjectPath, StringComparer.OrdinalIgnoreCase)))
                {
                    newArgs.Add(ArgumentCommandConstants.ShortProjectPath);
                }

                newArgs.Add(Environment.CurrentDirectory);
            }
            else
            {
                newArgs.Add(s);
            }
        }

        if (!newArgs.Contains(NameCommandConstants.Run, StringComparer.OrdinalIgnoreCase) &&
            !newArgs.Contains(NameCommandConstants.SanityCheck, StringComparer.OrdinalIgnoreCase) &&
            !newArgs.Contains(CommandConstants.NameOptionsFile, StringComparer.OrdinalIgnoreCase) &&
            !newArgs.Contains(NameCommandConstants.AnalyzerProviders, StringComparer.OrdinalIgnoreCase) &&
            (newArgs.Contains(ArgumentCommandConstants.ShortProjectPath, StringComparer.OrdinalIgnoreCase) ||
             newArgs.Contains(ArgumentCommandConstants.LongProjectPath, StringComparer.OrdinalIgnoreCase)))
        {
            newArgs.Insert(0, NameCommandConstants.Run);
        }

        return newArgs.ToArray();
    }

    private static string[] SetHelpArgumentIfNeeded(string[] args)
    {
        if (args.Length == 0)
        {
            return new[] { CommandConstants.ArgumentShortHelp };
        }

        if (args.Contains(NameCommandConstants.AnalyzerProviders, StringComparer.OrdinalIgnoreCase) &&
            args.Contains(NameCommandConstants.AnalyzerProvidersCleanupCache, StringComparer.OrdinalIgnoreCase))
        {
            return args;
        }

        if (!(args.Contains(ArgumentCommandConstants.ShortProjectPath, StringComparer.OrdinalIgnoreCase) ||
              args.Contains(ArgumentCommandConstants.LongProjectPath, StringComparer.OrdinalIgnoreCase)))
        {
            if (args.Contains(NameCommandConstants.SanityCheck, StringComparer.OrdinalIgnoreCase))
            {
                return new[] { NameCommandConstants.SanityCheck, CommandConstants.ArgumentShortHelp };
            }

            if (args.Contains(CommandConstants.NameOptionsFile, StringComparer.OrdinalIgnoreCase) &&
                (args.Contains(CommandConstants.NameOptionsFileCreate, StringComparer.OrdinalIgnoreCase) ||
                 args.Contains(CommandConstants.NameOptionsFileValidate, StringComparer.OrdinalIgnoreCase)))
            {
                return new[] { CommandConstants.NameOptionsFile, CommandConstants.ArgumentShortHelp };
            }

            if (args.Contains(NameCommandConstants.AnalyzerProviders, StringComparer.OrdinalIgnoreCase) &&
                args.Contains(NameCommandConstants.AnalyzerProvidersCollect, StringComparer.OrdinalIgnoreCase))
            {
                return new[] { NameCommandConstants.AnalyzerProviders, CommandConstants.ArgumentShortHelp };
            }

            if (args.Contains(NameCommandConstants.Run, StringComparer.OrdinalIgnoreCase))
            {
                return new[] { NameCommandConstants.Run, CommandConstants.ArgumentShortHelp };
            }
        }

        return args;
    }
}
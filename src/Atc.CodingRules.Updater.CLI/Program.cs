[assembly: CLSCompliant(false)]

// ReSharper disable InvertIf
namespace Atc.CodingRules.Updater.CLI;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        // Must happen before ServiceCollectionFactory.Create, which snapshots the console for the
        // logger. CommandAppFactory sets UTF-8 too, but it runs after that snapshot is taken, so on
        // a console using an OEM code page every emoji written by the logger degrades to '?'.
        System.Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        // Legacy names first: the steps below look for the current --project-path spelling.
        args = NormalizeLegacyOptionNames(args);
        args = SetProjectPathFromDotArgumentIfNeeded(args);
        args = SetHelpArgumentIfNeeded(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration
            .GetRequiredSection("ConsoleLogger")
            .Bind(consoleLoggerConfiguration);

        ProgramCsHelper.SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

        var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);

        var app = CommandAppFactory.CreateWithRootCommand<RootCommand>(serviceCollection);
        app.ConfigureCommands();

        return app.RunAsync(args);
    }

    /// <summary>
    /// Every top-level command. The shorthand rewrite inserts <c>run</c> only when none of these is
    /// present, so a command missing here is silently turned into <c>run &lt;command&gt;</c>.
    /// </summary>
    internal static readonly string[] CommandNames =
    [
        NameCommandConstants.Run,
        NameCommandConstants.SanityCheck,
        NameCommandConstants.Cpm,
        CommandConstants.NameOptionsFile,
        NameCommandConstants.AnalyzerProviders,
    ];

    /// <summary>
    /// The camelCase long option names released before options moved to kebab-case, mapped to
    /// their current name. Existing scripts use them, so they keep working, but only the
    /// kebab-case names are shown in <c>--help</c> and the docs.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> LegacyOptionNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--projectPath"] = ArgumentCommandConstants.LongProjectPath,
            ["--optionsPath"] = ArgumentCommandConstants.LongOptionsTarget,
            ["--projectTarget"] = ArgumentCommandConstants.LongProjectTarget,
            ["--useLatestMinorNugetVersion"] = ArgumentCommandConstants.LongUseLatestMinorNugetVersion,
            ["--useTemporarySuppressions"] = ArgumentCommandConstants.LongUseTemporarySuppressions,
            ["--temporarySuppressionPath"] = ArgumentCommandConstants.LongTemporarySuppressionPath,
            ["--temporarySuppressionAsExcel"] = ArgumentCommandConstants.LongTemporarySuppressionAsExcel,
            ["--buildFile"] = ArgumentCommandConstants.LongBuildFile,
            ["--buildConfiguration"] = ArgumentCommandConstants.LongBuildConfiguration,
            ["--buildProperty"] = ArgumentCommandConstants.LongBuildProperty,
            ["--fetchMode"] = ArgumentCommandConstants.LongFetchMode,
            ["--organizationName"] = ArgumentCommandConstants.LongOrganizationName,
            ["--repositoryName"] = ArgumentCommandConstants.LongRepositoryName,
            ["--forceNugetRefresh"] = ArgumentCommandConstants.LongForceNugetRefresh,
            ["--failOnChanges"] = ArgumentCommandConstants.LongFailOnChanges,
            ["--includeProviders"] = ArgumentCommandConstants.LongIncludeProviders,
            ["--excludeProviders"] = ArgumentCommandConstants.LongExcludeProviders,
        };

    /// <summary>
    /// Rewrites released camelCase option names to their kebab-case name, including the
    /// <c>--name=value</c> and <c>--name:value</c> forms. Anything else is left untouched.
    /// </summary>
    internal static string[] NormalizeLegacyOptionNames(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return args
            .Select(NormalizeLegacyOptionName)
            .ToArray();
    }

    private static string NormalizeLegacyOptionName(string argument)
    {
        if (!argument.StartsWith("--", StringComparison.Ordinal))
        {
            return argument;
        }

        var separatorIndex = argument.IndexOfAny(['=', ':']);
        var name = separatorIndex < 0 ? argument : argument[..separatorIndex];

        return LegacyOptionNames.TryGetValue(name, out var currentName)
            ? currentName + (separatorIndex < 0 ? string.Empty : argument[separatorIndex..])
            : argument;
    }

    internal static string[] SetProjectPathFromDotArgumentIfNeeded(
        string[] args)
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
                if (!(args.Contains(ArgumentCommandConstants.ShortProjectPath, StringComparer.OrdinalIgnoreCase) ||
                      args.Contains(ArgumentCommandConstants.LongProjectPath, StringComparer.OrdinalIgnoreCase)))
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

        if (!newArgs.Exists(x => CommandNames.Contains(x, StringComparer.OrdinalIgnoreCase)) &&
            (newArgs.Contains(ArgumentCommandConstants.ShortProjectPath, StringComparer.OrdinalIgnoreCase) ||
             newArgs.Contains(ArgumentCommandConstants.LongProjectPath, StringComparer.OrdinalIgnoreCase)))
        {
            newArgs.Insert(0, NameCommandConstants.Run);
        }

        if (!newArgs.Contains(CommandConstants.ArgumentLongVerbose, StringComparer.OrdinalIgnoreCase))
        {
            newArgs.Add(CommandConstants.ArgumentLongVerbose);
        }

        return [.. newArgs];
    }

    internal static string[] SetHelpArgumentIfNeeded(string[] args)
    {
        if (args.Length == 0)
        {
            return [CommandConstants.ArgumentShortHelp];
        }

        // An explicit help request already says which help to show.
        if (args.Contains(CommandConstants.ArgumentShortHelp, StringComparer.Ordinal) ||
            args.Contains(CommandConstants.ArgumentLongHelp, StringComparer.OrdinalIgnoreCase))
        {
            return args;
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
                return [NameCommandConstants.SanityCheck, CommandConstants.ArgumentShortHelp];
            }

            if (args.Contains(NameCommandConstants.Cpm, StringComparer.OrdinalIgnoreCase))
            {
                var verb = args.FirstOrDefault(x =>
                    x.Equals(NameCommandConstants.CpmValidate, StringComparison.OrdinalIgnoreCase) ||
                    x.Equals(NameCommandConstants.CpmMigrate, StringComparison.OrdinalIgnoreCase));

                return verb is null
                    ? [NameCommandConstants.Cpm, CommandConstants.ArgumentShortHelp]
                    : [NameCommandConstants.Cpm, verb, CommandConstants.ArgumentShortHelp];
            }

            if (args.Contains(CommandConstants.NameOptionsFile, StringComparer.OrdinalIgnoreCase) &&
                (args.Contains(CommandConstants.NameOptionsFileCreate, StringComparer.OrdinalIgnoreCase) ||
                 args.Contains(CommandConstants.NameOptionsFileValidate, StringComparer.OrdinalIgnoreCase)))
            {
                return [CommandConstants.NameOptionsFile, CommandConstants.ArgumentShortHelp];
            }

            if (args.Contains(NameCommandConstants.AnalyzerProviders, StringComparer.OrdinalIgnoreCase) &&
                args.Contains(NameCommandConstants.AnalyzerProvidersCollect, StringComparer.OrdinalIgnoreCase))
            {
                return [NameCommandConstants.AnalyzerProviders, CommandConstants.ArgumentShortHelp];
            }

            if (args.Contains(NameCommandConstants.Run, StringComparer.OrdinalIgnoreCase))
            {
                return [NameCommandConstants.Run, CommandConstants.ArgumentShortHelp];
            }
        }

        return args;
    }
}
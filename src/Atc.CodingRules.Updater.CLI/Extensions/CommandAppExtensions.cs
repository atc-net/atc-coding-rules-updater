namespace Atc.CodingRules.Updater.CLI.Extensions;

public static class CommandAppExtensions
{
    public static void ConfigureCommands(this CommandApp<RootCommand> app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Configure(config =>
        {
            ConfigureRunCommand(config);

            ConfigureSanityCheckCommand(config);

            config.AddBranch(CommandConstants.NameOptionsFile, ConfigureOptionsFileCommands());

            config.AddBranch(NameCommandConstants.AnalyzerProviders, ConfigureAnalyzerProvidersCommands());
        });
    }

    private static void ConfigureRunCommand(IConfigurator config)
        => config.AddCommand<RunCommand>(NameCommandConstants.Run)
            .WithDescription("Update the project folder with ATC coding rules and configurations")
            .WithExample(new[] { ".", CreateEquivalentToRun(8) })
            .WithExample(new[] { NameCommandConstants.Run, ".", CreateEquivalentToRun(4), })
            .WithExample(new[] { NameCommandConstants.Run, CreateArgumentProjectPathWithDot(), CreateEquivalentToRun(1) })
            .WithExample(new[] { NameCommandConstants.Run, CreateArgumentProjectPathWithTestFolder() })
            .WithExample(
                new[]
                {
                    NameCommandConstants.Run,
                    CreateArgumentProjectPathWithTestFolder(),
                    CreateArgumentProjectTarget(SupportedProjectTargetType.DotNetCore),
                    ArgumentCommandConstants.LongUseTemporarySuppressions,
                    ArgumentCommandConstants.LongOrganizationName, "MyCompany",
                    ArgumentCommandConstants.LongRepositoryName, "MyRepo",
                    CommandConstants.ArgumentShortVerbose,
                });

    private static void ConfigureSanityCheckCommand(IConfigurator config)
        => config.AddCommand<SanityCheckCommand>(NameCommandConstants.SanityCheck)
            .WithDescription("Sanity check the project files")
            .WithExample(new[] { NameCommandConstants.SanityCheck, ".", CreateEquivalentToSanityCheck(8) })
            .WithExample(new[] { NameCommandConstants.SanityCheck, CreateArgumentProjectPathWithTestFolder() })
            .WithExample(
                new[]
                {
                    NameCommandConstants.SanityCheck,
                    CreateArgumentProjectPathWithTestFolder(),
                    CreateArgumentProjectTarget(SupportedProjectTargetType.DotNetCore),
                    CommandConstants.ArgumentShortVerbose,
                });

    private static Action<IConfigurator<CommandSettings>> ConfigureOptionsFileCommands()
        => node =>
        {
            node.SetDescription("Commands for the options file 'atc-coding-rules-updater.json'");

            node
                .AddCommand<OptionsFileCreateCommand>(CommandConstants.NameOptionsFileCreate)
                .WithDescription("Create default options file 'atc-coding-rules-updater.json' if it doesn't exist")
                .WithExample(new[] { CreateArgumentCommandsOptionsFileWithCreate(), ".", CreateEquivalentToOptionsFileCreate(6), })
                .WithExample(new[] { CreateArgumentCommandsOptionsFileWithCreate(), CreateArgumentProjectPathWithDot(), CreateEquivalentToOptionsFileCreate(3), })
                .WithExample(new[] { CreateArgumentCommandsOptionsFileWithCreate(), CreateArgumentProjectPathWithTestFolder(), });

            node
                .AddCommand<OptionsFileValidateCommand>(CommandConstants.NameOptionsFileValidate)
                .WithDescription("Validate the options file 'atc-coding-rules-updater.json'")
                .WithExample(new[] { CreateArgumentCommandsOptionsFileWithValidate(), ".", CreateEquivalentToOptionsFileValidate(4), })
                .WithExample(new[] { CreateArgumentCommandsOptionsFileWithValidate(), CreateArgumentProjectPathWithTestFolder() });
        };

    private static Action<IConfigurator<CommandSettings>> ConfigureAnalyzerProvidersCommands()
        => node =>
        {
            node.SetDescription("Commands for analyzer providers");

            node
                .AddCommand<AnalyzerProvidersCollectCommand>(NameCommandConstants.AnalyzerProvidersCollect)
                .WithDescription("Collect base rules metadata from all Analyzer providers")
                .WithExample(new[] { CreateArgumentCommandsAnalyzerProvidersWithCollect(), ".", CreateEquivalentToAnalyzerProvidersCollect(6), })
                .WithExample(new[] { CreateArgumentCommandsAnalyzerProvidersWithCollect(), CreateArgumentProjectPathWithDot(), CreateEquivalentToAnalyzerProvidersCollect(3) })
                .WithExample(new[] { CreateArgumentCommandsAnalyzerProvidersWithCollect(), CreateArgumentProjectPathWithTestFolder() })
                .WithExample(
                    new[]
                    {
                        CreateArgumentCommandsAnalyzerProvidersWithCollect(),
                        CreateArgumentProjectPathWithTestFolder(),
                        CreateArgumentFetchMode(ProviderCollectingMode.ReCollect),
                        CommandConstants.ArgumentShortVerbose,
                    });

            node
                .AddCommand<AnalyzerProvidersCacheCleanupCommand>(NameCommandConstants.AnalyzerProvidersCleanupCache)
                .WithDescription("Cleanup cache from Analyzer providers")
                .WithExample(new[] { CreateArgumentCommandsAnalyzerProvidersWithCleanupCache() });
        };

    private static string CreateArgumentProjectPathWithDot()
        => $"{ArgumentCommandConstants.ShortProjectPath} .";

    private static string CreateArgumentProjectPathWithCurrentFolder()
        => $"{ArgumentCommandConstants.ShortProjectPath} [CurrentFolder]";

    private static string CreateArgumentProjectPathWithTestFolder()
        => @$"{ArgumentCommandConstants.ShortProjectPath} c:\temp\MyProject";

    private static string CreateArgumentProjectTarget(SupportedProjectTargetType targetType)
        => @$"{ArgumentCommandConstants.ShortProjectTarget} {targetType}";

    private static string CreateArgumentFetchMode(ProviderCollectingMode collectingMode)
        => @$"{ArgumentCommandConstants.LongFetchMode} {collectingMode}";

    private static string CreateArgumentCommandsAnalyzerProvidersWithCollect()
        => $"{NameCommandConstants.AnalyzerProviders} {NameCommandConstants.AnalyzerProvidersCollect}";

    private static string CreateArgumentCommandsOptionsFileWithCreate()
        => $"{CommandConstants.NameOptionsFile} {CommandConstants.NameOptionsFileCreate}";

    private static string CreateArgumentCommandsOptionsFileWithValidate()
        => $"{CommandConstants.NameOptionsFile} {CommandConstants.NameOptionsFileValidate}";

    private static string CreateArgumentCommandsAnalyzerProvidersWithCleanupCache()
        => $"{NameCommandConstants.AnalyzerProviders} {NameCommandConstants.AnalyzerProvidersCleanupCache}";

    private static string CreateEquivalentToRun(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{NameCommandConstants.Run} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string CreateEquivalentToSanityCheck(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{NameCommandConstants.SanityCheck} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string CreateEquivalentToOptionsFileCreate(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{CreateArgumentCommandsOptionsFileWithCreate()} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string CreateEquivalentToOptionsFileValidate(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{CreateArgumentCommandsOptionsFileWithValidate()} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string CreateEquivalentToAnalyzerProvidersCollect(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{CreateArgumentCommandsAnalyzerProvidersWithCollect()} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string PrefixSpaces(int indentSpaces, string value) => value.PadLeft(value.Length + indentSpaces);
}
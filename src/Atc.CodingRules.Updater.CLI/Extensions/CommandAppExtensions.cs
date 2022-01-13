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

            config.AddBranch(CommandConstants.NameAnalyzerProviders, ConfigureAnalyzerProvidersCommands());
        });
    }

    private static void ConfigureRunCommand(IConfigurator config)
        => config.AddCommand<RunCommand>(CommandConstants.NameRun)
            .WithDescription("Update the project folder with ATC coding rules and configurations")
            .WithExample(new[] { ".", CreateEquivalentToRun(8) })
            .WithExample(new[] { CommandConstants.NameRun, ".", CreateEquivalentToRun(4), })
            .WithExample(new[] { CommandConstants.NameRun, CreateArgumentProjectPathWithDot(), CreateEquivalentToRun(1) })
            .WithExample(new[] { CommandConstants.NameRun, CreateArgumentProjectPathWithTestFolder() })
            .WithExample(
                new[]
                {
                    CommandConstants.NameRun,
                    CreateArgumentProjectPathWithTestFolder(),
                    CreateArgumentProjectTarget(SupportedProjectTargetType.DotNetCore),
                    CommandConstants.ArgumentLongUseTemporarySuppressions,
                    CommandConstants.ArgumentLongOrganizationName, "MyCompany",
                    CommandConstants.ArgumentLongRepositoryName, "MyRepo",
                    CommandConstants.ArgumentShortVerbose,
                });

    private static void ConfigureSanityCheckCommand(IConfigurator config)
        => config.AddCommand<SanityCheckCommand>(CommandConstants.NameSanityCheck)
            .WithDescription("Sanity check the project files")
            .WithExample(new[] { CommandConstants.NameSanityCheck, ".", CreateEquivalentToSanityCheck(8) })
            .WithExample(new[] { CommandConstants.NameSanityCheck, CreateArgumentProjectPathWithTestFolder() })
            .WithExample(
                new[]
                {
                    CommandConstants.NameSanityCheck,
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
                .AddCommand<AnalyzerProvidersCollectCommand>(CommandConstants.NameAnalyzerProvidersCollect)
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
                .AddCommand<AnalyzerProvidersCacheCleanupCommand>(CommandConstants.NameAnalyzerProvidersCleanupCache)
                .WithDescription("Cleanup cache from Analyzer providers")
                .WithExample(new[] { CreateArgumentCommandsAnalyzerProvidersWithCleanupCache() });
        };

    private static string CreateArgumentProjectPathWithDot() => $"{CommandConstants.ArgumentShortProjectPath} .";

    private static string CreateArgumentProjectPathWithCurrentFolder() => $"{CommandConstants.ArgumentShortProjectPath} [CurrentFolder]";

    private static string CreateArgumentProjectPathWithTestFolder() => @$"{CommandConstants.ArgumentShortProjectPath} c:\temp\MyProject";

    private static string CreateArgumentProjectTarget(SupportedProjectTargetType targetType) => @$"{CommandConstants.ArgumentShortProjectTarget} {targetType}";

    private static string CreateArgumentFetchMode(ProviderCollectingMode collectingMode) => @$"{CommandConstants.ArgumentLongFetchMode} {collectingMode}";

    private static string CreateArgumentCommandsAnalyzerProvidersWithCollect() => $"{CommandConstants.NameAnalyzerProviders} {CommandConstants.NameAnalyzerProvidersCollect}";

    private static string CreateArgumentCommandsOptionsFileWithCreate() => $"{CommandConstants.NameOptionsFile} {CommandConstants.NameOptionsFileCreate}";

    private static string CreateArgumentCommandsOptionsFileWithValidate() => $"{CommandConstants.NameOptionsFile} {CommandConstants.NameOptionsFileValidate}";

    private static string CreateArgumentCommandsAnalyzerProvidersWithCleanupCache() => $"{CommandConstants.NameAnalyzerProviders} {CommandConstants.NameAnalyzerProvidersCleanupCache}";

    private static string CreateEquivalentToRun(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{CommandConstants.NameRun} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string CreateEquivalentToSanityCheck(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{CommandConstants.NameSanityCheck} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string CreateEquivalentToOptionsFileCreate(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{CreateArgumentCommandsOptionsFileWithCreate()} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string CreateEquivalentToOptionsFileValidate(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{CreateArgumentCommandsOptionsFileWithValidate()} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string CreateEquivalentToAnalyzerProvidersCollect(int indentSpaces)
        => PrefixSpaces(indentSpaces, $"(equivalent to '{CreateArgumentCommandsAnalyzerProvidersWithCollect()} {CreateArgumentProjectPathWithCurrentFolder()}')");

    private static string PrefixSpaces(int indentSpaces, string value) => value.PadLeft(value.Length + indentSpaces);
}
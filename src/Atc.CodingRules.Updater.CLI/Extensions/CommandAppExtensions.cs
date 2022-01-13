namespace Atc.CodingRules.Updater.CLI.Extensions
{
    public static class CommandAppExtensions
    {
        private const string TestProjectPath = @"c:\temp\MyProject";

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
                .WithExample(new[] { @$"{CommandConstants.NameRun} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath}" })
                .WithExample(new[] { @$"{CommandConstants.NameRun} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath} {CommandConstants.ArgumentLongProjectTarget} {SupportedProjectTargetType.DotNetCore} {CommandConstants.ArgumentLongUseTemporarySuppressions} {CommandConstants.ArgumentShortVerbose}" })
                .WithExample(new[] { @$"{CommandConstants.NameRun} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath} {CommandConstants.ArgumentLongProjectTarget} {SupportedProjectTargetType.DotNetCore} {CommandConstants.ArgumentLongUseTemporarySuppressions} {CommandConstants.ArgumentShortVerbose} {CommandConstants.ArgumentLongOrganizationName} MyCompany {CommandConstants.ArgumentLongRepositoryName} MyRepo" });

        private static void ConfigureSanityCheckCommand(IConfigurator config)
            => config.AddCommand<SanityCheckCommand>(CommandConstants.NameSanityCheck)
                .WithDescription("Sanity check the project files.")
                .WithExample(new[] { @$"{CommandConstants.NameSanityCheck} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath}" })
                .WithExample(new[] { @$"{CommandConstants.NameSanityCheck} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath} {CommandConstants.ArgumentLongProjectTarget} {SupportedProjectTargetType.DotNetCore} {CommandConstants.ArgumentShortVerbose}" });

        private static Action<IConfigurator<CommandSettings>> ConfigureOptionsFileCommands()
            => node =>
            {
                node.SetDescription("Commands for the options file 'atc-coding-rules-updater.json'");

                node
                    .AddCommand<OptionsFileCreateCommand>(CommandConstants.NameOptionsFileCreate)
                    .WithDescription("Create default options file 'atc-coding-rules-updater.json' if it doesn't exist")
                    .WithExample(new[] { @$"{CommandConstants.NameOptionsFile} {CommandConstants.NameOptionsFileCreate} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath}" });

                node
                    .AddCommand<OptionsFileValidateCommand>(CommandConstants.NameOptionsFileValidate)
                    .WithDescription("Validate the options file 'atc-coding-rules-updater.json'")
                    .WithExample(new[] { @$"{CommandConstants.NameOptionsFile} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath}" });
            };

        private static Action<IConfigurator<CommandSettings>> ConfigureAnalyzerProvidersCommands()
            => node =>
            {
                node.SetDescription("Commands for analyzer providers");

                node
                    .AddCommand<AnalyzerProvidersCollectCommand>(CommandConstants.NameAnalyzerProvidersCollect)
                    .WithDescription("Collect base rules metadata from all Analyzer providers")
                    .WithExample(new[] { @$"{CommandConstants.NameAnalyzerProviders} {CommandConstants.NameAnalyzerProvidersCollect} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath}" })
                    .WithExample(new[] { @$"{CommandConstants.NameAnalyzerProviders} {CommandConstants.NameAnalyzerProvidersCollect} {CommandConstants.ArgumentShortProjectPath} {TestProjectPath} {CommandConstants.ArgumentLongFetchMode} {ProviderCollectingMode.ReCollect} {CommandConstants.ArgumentShortVerbose}" });

                node
                    .AddCommand<AnalyzerProvidersCacheCleanupCommand>(CommandConstants.NameAnalyzerProvidersCleanupCache)
                    .WithDescription("Cleanup cache from Analyzer providers")
                    .WithExample(new[] { $"{CommandConstants.NameAnalyzerProviders} {CommandConstants.NameAnalyzerProvidersCleanupCache}" });
            };
    }
}
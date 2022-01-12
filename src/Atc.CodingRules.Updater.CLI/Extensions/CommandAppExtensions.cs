namespace Atc.CodingRules.Updater.CLI.Extensions
{
    public static class CommandAppExtensions
    {
        public static void ConfigureCommands(this CommandApp<RootCommand> app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.Configure(config =>
            {
                config.AddExample(new[] { @"-p c:\temp\MyProject" });
                config.AddExample(new[] { @"-p c:\temp\MyProject --projectTarget DotNetCore --useTemporarySuppressions -v" });
                config.AddExample(new[] { @"-p c:\temp\MyProject --projectTarget DotNetCore --useTemporarySuppressions -v --organizationName MyCompany --repositoryName MyRepo" });

                config.AddCommand<VersionCommand>("version")
                    .WithDescription("Display version for the ATC-Coding-Rules-Updater");

                config.AddCommand<SanityCheckCommand>("sanity-check")
                    .WithDescription("Sanity check the project files.")
                    .WithExample(new[] { @"sanity-check -p c:\temp\MyProject" })
                    .WithExample(new[] { @"sanity-check -p c:\temp\MyProject --projectTarget DotNetCore -v" });

                config.AddBranch("options-file", ConfigureOptionsFileCommands());
                config.AddBranch("analyzer-providers", ConfigureAnalyzerProvidersCommands());
            });
        }

        private static Action<IConfigurator<CommandSettings>> ConfigureOptionsFileCommands()
            => node =>
            {
                node.SetDescription("Commands for the options file 'atc-coding-rules-updater.json'");
                node
                    .AddCommand<OptionsFileCreateCommand>("create")
                    .WithDescription("Create default options file 'atc-coding-rules-updater.json' if it doesn't exist")
                    .WithExample(new[] { @"options-file create -p c:\temp\MyProject" });
                node
                    .AddCommand<OptionsFileValidateCommand>("validate")
                    .WithDescription("Validate the options file 'atc-coding-rules-updater.json'")
                    .WithExample(new[] { @"options-file -p c:\temp\MyProject" });
            };

        private static Action<IConfigurator<CommandSettings>> ConfigureAnalyzerProvidersCommands()
            => node =>
            {
                node.SetDescription("Commands for analyzer providers");
                node
                    .AddCommand<AnalyzerProvidersCollectCommand>("collect")
                    .WithDescription("Collect base rules metadata from all Analyzer providers")
                    .WithExample(new[] { @"analyzer-providers collect -p c:\temp\MyProject" })
                    .WithExample(new[] { @"analyzer-providers collect -p c:\temp\MyProject -fetchMode ReCollect -v" });
                node
                    .AddCommand<AnalyzerProvidersCacheCleanupCommand>("cache-cleanup")
                    .WithDescription("Cleanup cache from Analyzer providers")
                    .WithExample(new[] { "analyzer-providers cache-cleanup" });
            };
    }
}
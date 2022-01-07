using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Atc.CodingRules.Updater.CLI.Commands;
using Atc.Console.Spectre.Factories;
using Atc.Console.Spectre.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[assembly: CLSCompliant(false)]

namespace Atc.CodingRules.Updater.CLI
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            //args = new[]
            //{
            //    "-r", @"C:\Temp\sletmig",
            //   // "-v",
            //};

            args = WriteHelpIfNeeded(args);

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
            configuration.GetSection("ConsoleLogger").Bind(consoleLoggerConfiguration);

            SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

            var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);

            var app = CommandAppFactory2.CreateWithSingleCommand<RootCommand>(serviceCollection);

            return app.RunAsync(args);
        }

        private static string[] WriteHelpIfNeeded(string[] args)
        {
            if (!args.Any())
            {
                args = new[]
                {
                    "-h",
                };
            }

            return args;
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
}
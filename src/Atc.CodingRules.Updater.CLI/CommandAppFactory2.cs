using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Atc.CodingRules.Updater.CLI
{
    // TODO: Move Logic to ATC-Spectre-Console
    public static class CommandAppFactory2
    {
        public static CommandApp Create(ServiceCollection serviceCollection)
        {
            return Create(serviceCollection, Encoding.UTF8);
        }

        public static CommandApp Create(ServiceCollection serviceCollection, Encoding encoding)
        {
            Thread.CurrentThread.SetCulture(GlobalizationConstants.EnglishCultureInfo);
            System.Console.OutputEncoding = encoding;

            var registrar = new Console.Spectre.Factories.Infrastructure.TypeRegistrar(serviceCollection);
            var commandApp = new CommandApp(registrar);

            // TODO: See if this can be extracted
            var appName = Assembly
                .GetEntryAssembly()!
                .GetName()
                .Name;

            commandApp.Configure(config =>
            {
                config.SetApplicationName($"{appName}.exe");
            });

            return commandApp;
        }

        public static CommandApp<T> CreateWithSingleCommand<T>(ServiceCollection serviceCollection)
            where T : class, ICommand
        {
            Thread.CurrentThread.SetCulture(GlobalizationConstants.EnglishCultureInfo);
            System.Console.OutputEncoding = Encoding.UTF8;

            var registrar = new Console.Spectre.Factories.Infrastructure.TypeRegistrar(serviceCollection);
            var commandApp = new CommandApp<T>(registrar);

            // TODO: See if this can be extracted
            var appName = Assembly
                .GetEntryAssembly()!
                .GetName()
                .Name;

            commandApp.Configure(config =>
            {
                config.SetApplicationName($"{appName}.exe");
            });

            return commandApp;
        }
    }
}

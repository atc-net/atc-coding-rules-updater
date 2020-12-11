using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using Atc.CodingRules.Updater.CLI.Commands;
using Microsoft.Extensions.Hosting;

namespace Atc.CodingRules.Updater.CLI
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var builder = new HostBuilder();

            try
            {
                return await builder
                        .RunCommandLineApplicationAsync<RootCommand>(args)
                        .ConfigureAwait(false)
;           }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Colorful.Console.WriteLine($@"Error: {ex.InnerException.Message}", Color.Red);
                return ExitStatusCodes.Failure;
            }
            catch (Exception ex)
            {
                Colorful.Console.WriteLine($@"Error: {ex.Message}", Color.Red);
                return ExitStatusCodes.Failure;
            }
        }
    }
}
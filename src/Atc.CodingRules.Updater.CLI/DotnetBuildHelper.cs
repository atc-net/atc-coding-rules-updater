using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace Atc.CodingRules.Updater.CLI
{
    public static class DotnetBuildHelper
    {
        [SuppressMessage("Design", "MA0016:Prefer return collection abstraction instead of implementation", Justification = "OK.")]
        public static Dictionary<string, int> BuildAndCollectErrors(DirectoryInfo rootPath)
        {
            if (rootPath == null)
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            Tuple<string, string> buildResult = RunBuildCommand(rootPath);
            Dictionary<string, int> parsedErrors = ParseBuildOutput(buildResult);
            return parsedErrors;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "OK.")]
        private static Tuple<string, string> RunBuildCommand(DirectoryInfo rootPath)
        {
            Process process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = rootPath.FullName,
                    FileName = "dotnet.exe",
                    Arguments = "build -c Release -v q -clp:NoSummary",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
            };

            process.Start();
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            Console.WriteLine(standardError);
            process.WaitForExit();
            return Tuple.Create(standardOutput, standardError);
        }

        [SuppressMessage("Performance", "MA0023:Add RegexOptions.ExplicitCapture", Justification = "OK.")]
        private static Dictionary<string, int> ParseBuildOutput(Tuple<string, string> buildResult)
        {
            const string? regexPattern = @": error ([A-Z]\S+?): (.*) \[";
            var errors = new Dictionary<string, int>(StringComparer.Ordinal);

            var regex = new Regex(
                regexPattern,
                RegexOptions.Multiline | RegexOptions.Compiled,
                TimeSpan.FromMinutes(2));

            var matchCollection = regex.Matches(buildResult.Item1);
            foreach (Match match in matchCollection)
            {
                if (match.Groups.Count != 3)
                {
                    continue;
                }

                var key = match.Groups[1].Value;
                if (errors.ContainsKey(key))
                {
                    errors[key] = errors[key] + 1;
                }
                else
                {
                    errors.Add(key, 1);
                }
            }

            return errors;
        }
    }
}
global using System.Data;
global using System.Globalization;
global using System.Reflection;
global using System.Text;
global using System.Text.RegularExpressions;

global using Atc.CodingRules.AnalyzerProviders.Models;
global using Atc.CodingRules.Updater.CentralPackageManagement;
global using Atc.CodingRules.Updater.CentralPackageManagement.Migration;
global using Atc.CodingRules.Updater.CLI;
global using Atc.CodingRules.Updater.CLI.Commands;
global using Atc.CodingRules.Updater.CLI.Commands.Settings;
global using Atc.CodingRules.Updater.CLI.Helpers;
global using Atc.CodingRules.Updater.CLI.Models;
global using Atc.CodingRules.Updater.CLI.Models.Options;
global using Atc.CodingRules.Updater.DirectoryBuildProps;
global using Atc.CodingRules.Updater.EditorConfig;
global using Atc.CodingRules.Updater.Models;
global using Atc.CodingRules.Updater.MsBuild;
global using Atc.CodingRules.Updater.SanityCheck;
global using Atc.DotNet;

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;

global using Spectre.Console.Cli;
global using System;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.Data;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.Text;

global using Atc.CodingRules.AnalyzerProviders;
global using Atc.CodingRules.AnalyzerProviders.Models;
global using Atc.CodingRules.Updater.CLI.Commands;
global using Atc.CodingRules.Updater.CLI.Commands.DescriptionAttributes;
global using Atc.CodingRules.Updater.CLI.Commands.Settings;
global using Atc.CodingRules.Updater.CLI.Extensions;
global using Atc.CodingRules.Updater.CLI.Models.Options;
global using Atc.Console.Spectre;
global using Atc.Console.Spectre.CommandSettings;
global using Atc.Console.Spectre.Factories;
global using Atc.Console.Spectre.Helpers;
global using Atc.Console.Spectre.Logging;
global using Atc.Helpers;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;

global using Spectre.Console;
global using Spectre.Console.Cli;
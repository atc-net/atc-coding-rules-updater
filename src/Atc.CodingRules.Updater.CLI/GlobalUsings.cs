global using System;
global using System.Collections.ObjectModel;
global using System.Data;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.Text;
global using System.Text.Json;

global using Atc.CodingRules.AnalyzerProviders;
global using Atc.CodingRules.AnalyzerProviders.Models;
global using Atc.CodingRules.Updater.CLI.Commands;
global using Atc.CodingRules.Updater.CLI.Commands.DescriptionAttributes;
global using Atc.CodingRules.Updater.CLI.Commands.Settings;
global using Atc.CodingRules.Updater.CLI.Models;
global using Atc.Console.Spectre.Factories;
global using Atc.Console.Spectre.Logging;
global using Atc.Serialization;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;

global using Spectre.Console.Cli;
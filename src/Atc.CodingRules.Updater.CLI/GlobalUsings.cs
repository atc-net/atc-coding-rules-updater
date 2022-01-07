global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Data;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;

global using Atc.CodingRules.AnalyzerProviders;
global using Atc.CodingRules.AnalyzerProviders.Models;
global using Atc.CodingRules.Updater.CLI.Commands;
global using Atc.CodingRules.Updater.CLI.Commands.Settings;
global using Atc.CodingRules.Updater.CLI.Models;
global using Atc.Console.Spectre.Factories;
global using Atc.Console.Spectre.Logging;
global using Atc.Data.Models;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;

global using Spectre.Console.Cli;
### General Project Info
[![Github top language](https://img.shields.io/github/languages/top/atc-net/atc-coding-rules-updater)](https://github.com/atc-net/atc-coding-rules-updater)
[![Github stars](https://img.shields.io/github/stars/atc-net/atc-coding-rules-updater)](https://github.com/atc-net/atc-coding-rules-updater)
[![Github forks](https://img.shields.io/github/forks/atc-net/atc-coding-rules-updater)](https://github.com/atc-net/atc-coding-rules-updater)
[![Github size](https://img.shields.io/github/repo-size/atc-net/atc-coding-rules-updater)](https://github.com/atc-net/atc-coding-rules-updater)
[![Issues Open](https://img.shields.io/github/issues/atc-net/atc-coding-rules-updater.svg?logo=github)](https://github.com/atc-net/atc-coding-rules-updater/issues)

### Packages
[![Github Version](https://img.shields.io/static/v1?logo=github&color=blue&label=github&message=latest)](https://github.com/orgs/atc-net/packages?repo_name=atc-coding-rules-updater)
[![NuGet Version](https://img.shields.io/nuget/v/atc-coding-rules-updater.svg?logo=nuget)](https://www.nuget.org/profiles/atc-net)

### Build Status
![Pre-Integration](https://github.com/atc-net/atc-coding-rules-updater/workflows/Pre-Integration/badge.svg)
![Post-Integration](https://github.com/atc-net/atc-coding-rules-updater/workflows/Post-Integration/badge.svg)
![Release](https://github.com/atc-net/atc-coding-rules-updater/workflows/Release/badge.svg)

### Code Quality
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=atc-coding-rules-updater&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=atc-coding-rules-updater)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=atc-coding-rules-updater&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=atc-coding-rules-updater)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=atc-coding-rules-updater&metric=security_rating)](https://sonarcloud.io/dashboard?id=atc-coding-rules-updater)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=atc-coding-rules-updater&metric=bugs)](https://sonarcloud.io/dashboard?id=atc-coding-rules-updater)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=atc-coding-rules-updater&metric=vulnerabilities)](https://sonarcloud.io/dashboard?id=atc-coding-rules-updater)

# ATC.Net Coding rules updater

This repository contains the CLI tool, which can be used to maintain the `coding-rules` in a project, where [atc-coding-rules](https://github.com/atc-net/atc-coding-rules) have been utilized.

* Read more about [atc-coding-rules](https://github.com/atc-net/atc-coding-rules)
* Master rule files are located [here](https://github.com/atc-net/atc-coding-rules/tree/main/distribution)

# CLI Tool

The Atc.CodingRules.Updater.CLI library is available through a cross platform command line application.

## Requirements

- .NET Core 3.1 runtime

## Installation

The tool can be installed as a .NET Core global tool by the following command

```powershell
dotnet tool install --global atc-coding-rules-updater
```

or by following the instructions [here](https://www.nuget.org/packages/atc-coding-rules-updater/) to install a specific version of the tool.

A successful installation will output something like

```powershell
The tool can be invoked by the following command: atc-coding-rules-updater
Tool 'atc-coding-rules-updater' (version '1.0.xxx') was successfully installed.`
```

## Update

The tool can be updated by following command

```powershell
dotnet tool update --global atc-coding-rules-updater
```

## Usage

Since the tool is published as a .NET Core Tool, it can be launched from anywhere using any shell or command-line interface by calling **atc-coding-rules-updater**. The help information is displayed when providing the `--help` argument to **atc-coding-rules-updater**

```powershell
$ atc-coding-rules-updater --help
```

## Example

Having a project folder in c:\code\MyProject where the .sln file for C# projects exists in the root, run the following command

```powershell
$ atc-coding-rules-updater -r c:\code\MyProject -v true
```

Running the command above produces the following output

```powershell
        ___  ______  _____        ___          __                                 __        __
       / _ |/_  __/ / ___/ ____  / _ \ __ __  / / ___   ___      __ __   ___  ___/ / ___ _ / /_ ___   ____
      / __ | / /   / /__  /___/ / , _// // / / / / -_) (_-<     / // /  / _ \/ _  / / _ `// __// -_) / __/
     /_/ |_|/_/    \___/       /_/|_| \_,_/ /_/  \__/ /___/     \_,_/  / .__/\_,_/  \_,_/ \__/ \__/ /_/
                                                                      /_/

FileUpdate # Information: common.props updated - Remember to change CompanyName in the file
FileUpdate # Debug: code-analysis.props updated
FileUpdate # Debug: .editorconfig updated
FileUpdate # Debug: src/.editorconfig updated
FileUpdate # Debug: src/Directory.Build.props updated
FileUpdate # Debug: test/.editorconfig updated
FileUpdate # Debug: test/Directory.Build.props updated
FileUpdate # Debug: sample/.editorconfig updated
FileUpdate # Debug: sample/Directory.Build.props updated

Update is OK.
```

## Options file schema / example

The tool has an optional options parameter, which can be used to control the paths for persisting the .editorconfigs and props files. This can be applied as follows `--optionsPath 'C:\Temp\atc-coding-rules-updater.json'`

**atc-coding-rules-updater.json example**

```json
{
	"Mappings": {
		"Sample": {
			"Paths": [
				"C:\\Temp\\MyProject\\sample1",
				"C:\\Temp\\MyProject\\sample2"
			]
		},
		"Src": {
			"Paths": [
				"C:\\Temp\\MyProject\\src"
			]
		},
		"Test": {
			"Paths": [
				"C:\\Temp\\MyProject\\test"
			]
		}
	}
}
```

or

```json
{
	"Mappings": {
		"Src": { "Paths": [ "source" ] },
		"Test": { "Paths": [ "tests" ] }
	}
}
```

**Note:** If there is a `atc-coding-rules-updater.json` file present in the root folder (given by options `--outputRootPath` /  `-r`), then it will automatically be found and used.

# CLI Tool Usage from powershell

To ensure that the latest version of the CLI tool `atc-coding-rules-updater` is being used, the following methodology can be used:

1) Download the 2 files from `sample` into a project root folder.
2) Modify the `atc-coding-rules-updater.json` to the projects specific needs.
3) Run `atc-coding-rules-updater.ps1` from powershell

# Deep dive in what `atc-coding-rules-updater` actual do and don't do

The `atc-coding-rules-updater` downloads files from the [atc-coding-rules repository's distribution folder](https://github.com/atc-net/atc-coding-rules/tree/main/distribution).

From here it works with 2 concepts:
* Scaffolding files (`.editorconfig` and `Directory.Build.props`) - if a file does not exist - it creates a copy.
* Updating files (`.editorconfig`) - if a file exist - updates the files first content part and does not touch the second content part.
    * First content part is related to rules above the line `# Custom - Code Analyzers Rules` - and will be updated - herafter known as ATC-part.
    * Second content part is related to rules below the line `# Custom - Code Analyzers Rules` - and will not be touched - herafter known as Customer-part.

## A use case-scenario for coding rules structure setups - Scenario A

In scenario A we have root where `src` and `test` destination is defined as:
```json
{
	"Mappings": {
		"Src": { "Paths": [ "src" ] },
		"Test": { "Paths": [ "test" ] }
	}
}
```

When the `atc-coding-rules-update` is exceuted first time, the following happens (see legend for explanation):

- ![#70AD47](https://via.placeholder.com/15/70AD47/000000?text=+) arrows indicate files created in `root` folder.
- ![#00B0F0](https://via.placeholder.com/15/00B0F0/000000?text=+) arrows indicate files created in `src` folder.
- ![#7030A0](https://via.placeholder.com/15/7030A0/000000?text=+) arrows indicate files created in `test` folder.

![Img](docs/scenario-a-first-run.png)

## A use case-scenario for coding rules structure setups - Scenario B

In this scenario we have root where `src` and `test` destination is defined as:
```json
{
	"Mappings": {
		"Src": { "Paths": [
            "MyDemo.Gui",
            "MyDemo.SharedContracts",
            "MyDemo.WebApi"
            ] },
		"Test": { "Paths": [
            "MyDemo.Gui.Tests",
            "MyDemo.SharedContracts.Tests",
            "MyDemo.WebApi.Tests"
            ] }
	}
}
```

When the `atc-coding-rules-update` is executed first time, the following happens (see legend for explanation):

- ![#70AD47](https://via.placeholder.com/15/70AD47/000000?text=+) arrows indicate files created in `root` folder.
- ![#00B0F0](https://via.placeholder.com/15/00B0F0/000000?text=+) arrows indicate files created in `src` folder.
- ![#7030A0](https://via.placeholder.com/15/7030A0/000000?text=+) arrows indicate files created in `test` folder.

![Img](docs/scenario-b-first-run.png)

## For both use-case scenarios

For both scenario A and scenario B, when the `atc-coding-rules-update` is executed a second time it will only update `.editorconfig` file. And as previously stated, it will only update the ATC-part of these files.

![Img](docs/scenario-ab-second-run.png)

# Temporary suppressions

When starting with ATC rules on an existing project, the general recommendation is to initially suppress all failing rules in your Custom section in order to get back to ✅ state for the project, and then later clean up the code and remove the suppressions one by one in nice clean commits.

The challenge is that this initial suppression list takes a long time to hand-write. In order to circumvent this tedious process, some extra flags have been added to the CLI to allow for auto-generation of these suppression lines. When utilizing these new options, the updater process will run a dotnet build on your project and extract any errors and create suppressions for these along with a count of how many occurences are present. The CLI will also add additional information, e.g: Category, Title, Link to the rule being broken.

The two CLI options for generating temporary suppressions are as follows.

```json
--useTemporarySuppressions      Use temporary suppressions from build - appends to .editorconfig - unless temporarySuppressionPath is set
--temporarySuppressionPath      Optional path to temporary suppressions file - if not set .editorconfig file is used
--temporarySuppressionAsExcel   Optional - save temporary suppressions file as Excel (.xlsx)
```
Below an example of the auto-generated supressions is shown:

```json
##########################################
# ATC temporary suppressions
# generated @ 21. juni 2021 02:33:34
# Please fix all generated temporary suppressions
# either by code changes or move the
# suppressions one by one to the relevant
# 'Custom - Code Analyzers Rules' section.
##########################################

# Microsoft.CodeAnalysis.NetAnalyzers
dotnet_diagnostic.CA1303.severity = none			# 1 occurrence - Do not pass literals as localized parameters - https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1303
dotnet_diagnostic.CA1707.severity = none			# 1 occurrence - Identifiers should not contain underscores - https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1707
dotnet_diagnostic.CA1801.severity = none			# 1 occurrence - Review unused parameters - https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1801

# SonarAnalyzer.CSharp
dotnet_diagnostic.S1118.severity = none				# 1 occurrence - Category: 'Code Smell' - Utility classes should not have public constructors - https://rules.sonarsource.com/csharp/RSPEC-1118

# StyleCop.Analyzers
dotnet_diagnostic.SA1400.severity = none			# 2 occurrences - Category: 'Maintainability' - Access modifier must be declared - https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1400.md
```

### Build

When using the `--useTemporarySuppressions` option, a `dotnet.exe build` will be executed (up to 10 times depending on the complexity of the solution).
Hence a requirement is that dotnet.exe can be called from the root path.
If there are multiple solutions (.sln) files in the root folder, the `--buildFile` option will then be required as a parameter when calling the CLI tool.

```json
--buildFile                 Optional path to solution/project file - required when multiple .sln files exists in root path
```

# The workflow setup for this repository
[Read more on Git-Flow](docs/GitFlow.md)

# Contributing

Please refer to each project's style and contribution guidelines for submitting patches and additions. In general, we follow the "fork-and-pull" Git workflow. [Read more here](https://gist.github.com/Chaser324/ce0505fbed06b947d962).

 1. **Fork** the repo on GitHub
 2. **Clone** the project to your own machine
 3. **Commit** changes to your own branch
 4. **Push** your work back up to your fork
 5. Submit a **Pull request** so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!

# Coding Guidelines

This repository is adapting the [ATC-Coding-Rules](https://github.com/atc-net/atc-coding-rules) which is defined and based on .editorconfig's and a range of Roslyn Analyzers.

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
The tool can be invoked by the following command: atc-api
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
FileUpdate # Debug: src/directory.build.props updated
FileUpdate # Debug: src/directory.build.targets updated
FileUpdate # Debug: test/.editorconfig updated
FileUpdate # Debug: test/directory.build.props updated
FileUpdate # Debug: test/directory.build.targets updated
FileUpdate # Debug: sample/.editorconfig updated
FileUpdate # Debug: sample/directory.build.props updated
FileUpdate # Debug: sample/directory.build.targets updated

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

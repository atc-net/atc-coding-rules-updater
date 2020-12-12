# ATC.Net Coding rule update

This repository contains the CLI tool that can maintain the `coding-rules` in you project defined from `atc-coding-rules`.

* Read more about the [atc-coding-rules](https://github.com/atc-net/atc-coding-rules)
* Master rule files is located in [atc-coding-rules distribution](https://github.com/atc-net/atc-coding-rules/tree/main/distribution)

# CLI Tool Usage

The Atc.CodingRules.Updater.CLI library mentioned above is available through a cross platform command line application.

**Requirements**
- .NET Core 3.1 runtime

## Install:

The tool can be installed as a .NET Core global tool that you can call from the shell / command line
```
dotnet tool install --global atc-coding-rules-updater
```
or by following the instructions [here](https://www.nuget.org/packages/atc-coding-rules-updater/) to install a specific version of the tool.

A successful installation will output something like:
```
You can invoke the tool using the following command: atc-api
Tool 'atc-coding-rules-updater' (version '1.0.xxx') was successfully installed.`
```

## Update:

The tool can also be updated by following command.
```
dotnet tool update --global atc-coding-rules-updater
```

## Usage

Since the tool is published as a .NET Core Tool, it can be launched from anywhere using any command line interface by calling **atc-coding-rules-updater**. The help information is displayed using the `--help` argument to **atc-coding-rules-updater**

```
$ atc-coding-rules-updater --help
```

## Example

If you have a project folder in c:\code\MyProject where you have or will have you .sln file for C# projects.

Then do the following:

```
$ atc-coding-rules-updater -r c:\code\MyProject -v true
```

Running the command above produces the following output:

```
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

[![NuGet Version](https://img.shields.io/nuget/v/atc-coding-rules-updater.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc-coding-rules-updater)

# ATC-NET Coding rules updater

This repository contains a CLI tool, which can be used to maintain `coding-rules` in a project, where [atc-coding-rules](https://github.com/atc-net/atc-coding-rules) has been utilized.

* Read about [atc-coding-rules](https://github.com/atc-net/atc-coding-rules)
* Master rule files [information](https://github.com/atc-net/atc-coding-rules/tree/main/distribution)

## Table of Contents
- [ATC-NET Coding rules updater](#atc-net-coding-rules-updater)
  - [Table of Contents](#table-of-contents)
  - [CLI Tool](#cli-tool)
    - [Requirements](#requirements)
    - [Installation](#installation)
    - [Update](#update)
    - [Usage](#usage)
      - [Option --help](#option---help)
      - [Command run](#command-run)
      - [Command sanity-check](#command-sanity-check)
      - [Command cpm](#command-cpm)
      - [Command analyzer-providers](#command-analyzer-providers)
      - [Command options-file](#command-options-file)
    - [Example](#example)
  - [Options file schema / example](#options-file-schema--example)
    - [atc-coding-rules-updater.json example 1](#atc-coding-rules-updaterjson-example-1)
    - [atc-coding-rules-updater.json example 2](#atc-coding-rules-updaterjson-example-2)
    - [atc-coding-rules-updater.json example 3](#atc-coding-rules-updaterjson-example-3)
    - [atc-coding-rules-updater.json default](#atc-coding-rules-updaterjson-default)
  - [CLI Tool Usage from powershell](#cli-tool-usage-from-powershell)
  - [Deep dive in what `atc-coding-rules-updater` actual does and doesn't do](#deep-dive-in-what-atc-coding-rules-updater-actual-does-and-doesnt-do)
    - [A use case-scenario for coding rules structure setups - Scenario A](#a-use-case-scenario-for-coding-rules-structure-setups---scenario-a)
    - [A use case-scenario for coding rules structure setups - Scenario B](#a-use-case-scenario-for-coding-rules-structure-setups---scenario-b)
    - [For both use-case scenarios](#for-both-use-case-scenarios)
  - [Central Package Management (Directory.Packages.props)](#central-package-management-directorypackagesprops)
  - [Temporary suppressions](#temporary-suppressions)
    - [Build](#build)
  - [How to contribute](#how-to-contribute)

## CLI Tool

The Atc.CodingRules.Updater.CLI library is available through a cross platform command line application.

### Requirements

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

The CLI itself targets .NET 10. The `--project-target` option lets you point at older runtimes — supported values are `DotNetCore`, `DotNet5`, `DotNet6`, `DotNet7`, `DotNet8`, `DotNet9`, `DotNet10` (default), `DotNet11`.

### Installation

The tool can be installed as a .NET Core global tool by the following command

```powershell
dotnet tool install --global atc-coding-rules-updater --ignore-failed-sources
```

or by following the instructions [here](https://www.nuget.org/packages/atc-coding-rules-updater/) to install a specific version of the tool.

A successful installation will output something like

```powershell
The tool can be invoked by the following command: atc-coding-rules-updater
Tool 'atc-coding-rules-updater' (version '2.0.xxx') was successfully installed.`
```

### Update

The tool can be updated by following command

```powershell
dotnet tool update --global atc-coding-rules-updater --ignore-failed-sources
```

### Usage

Since the tool is published as a .NET Tool, it can be launched from anywhere using any shell or command-line interface by calling **atc-coding-rules-updater**. The help information is displayed when providing the `--help` argument to **atc-coding-rules-updater**

Commands are named `<noun> <verb>` (for example `cpm validate`, `options-file create`), and options are kebab-case
(for example `--project-path`, `--options-path`). The camelCase option names from earlier versions
(`--projectPath`, `--optionsPath`, `--useTemporarySuppressions`, …) still work, so existing scripts keep running,
but only the kebab-case names are documented.

#### Option <span style="color:yellow">--help</span>
```powershell
atc-coding-rules-updater --help


USAGE:
    atc-coding-rules-updater.exe [OPTIONS]

OPTIONS:
    -h, --help       Prints help information
        --verbose    Use verbose for more debug/trace information
    -v, --version    Display version

COMMANDS:
    run                   Update the project folder with ATC coding rules and configurations
    sanity-check          Sanity check the project files
    options-file          Commands for the options file 'atc-coding-rules-updater.json'
    analyzer-providers    Commands for analyzer providers
```

#### Command <span style="color:yellow">run</span>
```powershell
USAGE:
    atc-coding-rules-updater.exe run [OPTIONS]

EXAMPLES:
    atc-coding-rules-updater.exe .         (equivalent to 'run -p [CurrentFolder]')
    atc-coding-rules-updater.exe run .     (equivalent to 'run -p [CurrentFolder]')
    atc-coding-rules-updater.exe run -p .  (equivalent to 'run -p [CurrentFolder]')
    atc-coding-rules-updater.exe run -p c:\temp\MyProject
    atc-coding-rules-updater.exe run -p c:\temp\MyProject -t DotNetCore --use-temporary-suppressions  --organization-name
MyCompany  --repository-name MyRepo --verbose

OPTIONS:
    -h, --help                                                       Prints help information
        --verbose                                                    Use verbose for more debug/trace information
    -p, --project-path <PROJECTPATH>                                 Path to the project directory (default current
                                                                     diectory)
    -o, --options-path [OPTIONSPATH]                                 Path to an optional options json-file
    -t, --project-target [PROJECTTARGET]                             Sets the ProjectTarget. Valid values are: DotNetCore,
                                                                     DotNet5, DotNet6, DotNet7, DotNet8, DotNet9,
                                                                     DotNet10 (default), DotNet11
        --use-latest-minor-nuget-version                             Bump PackageReferences in Directory.Build.props to the
                                                                     latest version within the same major version.
                                                                     (default true)
    -s, --use-temporary-suppressions                                 Build the project, collect analyzer errors, and write
                                                                     temporary suppressions. Appended to .editorconfig
                                                                     unless --temporary-suppression-path is set.
                                                                     (default false)
        --temporary-suppression-path [TEMPORARYSUPPRESSIONPATH]      Optional output directory for the
                                                                     temporary-suppressions file. When set, the
                                                                     .editorconfig is not modified
        --temporary-suppression-as-excel                             Write the temporary-suppressions file as Excel (.xlsx)
                                                                     instead of plain text. (default false)
        --build-file [BUILDFILE]                                     Solution (.sln/.slnx) or project (.csproj) file to
                                                                     build. Required when multiple solution files exist in
                                                                     --project-path
        --build-configuration [BUILDCONFIGURATION]                   Configuration used for the temporary-suppression
                                                                     builds - Release or Debug. Use Debug when a
                                                                     Release-only target, such as an obfuscator, prevents
                                                                     the build from completing. (default Release)
        --build-property <BUILDPROPERTY>                             MSBuild property forwarded to the
                                                                     temporary-suppression builds as -p:Name=Value.
                                                                     Repeatable.
        --organization-name [ORGANIZATIONNAME]                       Organization name to substitute into the
                                                                     <OrganizationName> placeholder in Directory.Build.props
        --repository-name [REPOSITORYNAME]                           Repository name to substitute into the
                                                                     <RepositoryName> placeholder in Directory.Build.props
        --dry-run                                                    Preview mode: log what would be created or updated
                                                                     without writing any files. Skips the
                                                                     temporary-suppression build loop. (default false)
        --force-nuget-refresh                                        Ask the ATC API to re-read package versions from
                                                                     nuget.org instead of serving its 12-hour cache. Use
                                                                     when a just-published version is not being picked up.
                                                                     (default false)
        --fail-on-changes                                            Exit with a non-zero code when any file was created or
                                                                     updated. Combine with --dry-run to gate CI on "coding
                                                                     rules are current" without writing anything.
                                                                     (default false)
        --json                                                       Emit a machine-readable JSON summary on stdout
                                                                     (per-file outcomes, package bumps, props drift)
                                                                     instead of log output. Useful for CI.
        --central-package-management [CENTRALPACKAGEMANAGEMENT]      Central Package Management (Directory.Packages.props)
                                                                     handling: Auto detects it, Disabled always uses the
                                                                     non-CPM behaviour. (default Auto)
```

##### Gating CI on "coding rules are current"

`--fail-on-changes` combined with `--dry-run` writes nothing and returns `1` as soon as any file
would be created or updated, so a pull request can be blocked without the PR-creating workflow:

```powershell
atc-coding-rules-updater run -p . --dry-run --fail-on-changes
```

Reported-but-not-applied drift does **not** trip it. Drift is never applied by the tool, so a
re-run would not resolve it and failing on it would leave the build permanently red.

##### Machine-readable output

`--json` writes a single JSON document to stdout and nothing else, so it can be piped straight
into `jq`:

```json
{
  "DryRun": true,
  "Files": [
    { "Area": "root", "File": ".editorconfig", "Path": "...", "Outcome": "Unchanged" },
    { "Area": "root", "File": "Directory.Build.props", "Path": "...", "Outcome": "Updated" }
  ],
  "PackageBumps": [
    { "PackageId": "Meziantou.Analyzer", "FromVersion": "3.0.135", "ToVersion": "3.0.136" }
  ],
  "Drift": [
    {
      "Area": "src",
      "PackagesOnlyInDistribution": [],
      "PackagesOnlyInLocalFile": [ "Nerdbank.GitVersioning" ],
      "PropertiesOnlyInDistribution": []
    }
  ],
  "HasChanges": true
}
```

`Outcome` is one of `Unchanged`, `Created`, `Updated` or `Skipped`. `Skipped` means the
distribution had nothing to offer for that path; `Unchanged` means the local file was already
correct. Under `--dry-run` the same values describe what *would* have happened.

Two limitations of this mode:

- **No diagnostics.** Log output is suppressed so that stdout contains only the JSON document, so
  `--verbose` has no effect when `--json` is set. Drop `--json` to debug a run. Per-provider and
  per-file problems still surface — failures appear as an `Error` property on the document.
- **No prompting.** `--organization-name` and `--repository-name` must be passed explicitly when
  those placeholders are still present, since prompting would hang waiting on stdin.

**Note on `--use-latest-minor-nuget-version`:** despite the name, the bump is bounded by the **major**
version, not the minor — a package on `3.0.54` will be moved to the newest `3.x`, but never to `4.0.0`.

**Exit codes:** `run` and `sanity-check` return `0` on success and `1` on failure. `sanity-check`
returns `1` whenever any error-severity diagnostic is found, in both the default and `--json`
output modes, so it can be used directly as a CI gate.

#### Command <span style="color:yellow">sanity-check</span>
```powershell
USAGE:
    atc-coding-rules-updater.exe sanity-check [OPTIONS]

EXAMPLES:
    atc-coding-rules-updater.exe sanity-check .         (equivalent to 'sanity-check -p [CurrentFolder]')
    atc-coding-rules-updater.exe sanity-check -p c:\temp\MyProject
    atc-coding-rules-updater.exe sanity-check -p c:\temp\MyProject -t DotNetCore --verbose

OPTIONS:
    -h, --help                              Prints help information
        --verbose                           Use verbose for more debug/trace information
    -p, --project-path <PROJECTPATH>        Path to the project directory (default current diectory)
    -o, --options-path [OPTIONSPATH]        Path to an optional options json-file
    -t, --project-target [PROJECTTARGET]    Sets the ProjectTarget. Valid values are: DotNetCore, DotNet5, DotNet6, DotNet7, DotNet8, DotNet9, DotNet10 (default), DotNet11
        --json                              Emit a machine-readable JSON summary on stdout (Severity / Code / Message / FilePath per diagnostic). Useful for CI
```

#### Command <span style="color:yellow">cpm</span>

Commands for NuGet [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
(`Directory.Packages.props`). `cpm migrate` is the only way the updater turns Central Package Management on - `run`
never does. See [Central Package Management](#central-package-management-directorypackagesprops) for what it changes
and how it protects the repository.

```powershell
USAGE:
    atc-coding-rules-updater.exe cpm validate [OPTIONS]
    atc-coding-rules-updater.exe cpm migrate [OPTIONS]

EXAMPLES:
    atc-coding-rules-updater.exe cpm validate -p .
    atc-coding-rules-updater.exe cpm validate -p . --json
    atc-coding-rules-updater.exe cpm migrate -p . --dry-run
    atc-coding-rules-updater.exe cpm migrate -p . --build-file MySolution.slnx

COMMANDS:
    validate    Check whether the project folder can be migrated to Central Package Management: show the plan and
                every blocker, write nothing
    migrate     Migrate the project folder to Central Package Management, verified by restoring before and after

OPTIONS (both commands):
    -h, --help                                        Prints help information
        --verbose                                     Use verbose for more debug/trace information
    -p, --project-path <PROJECTPATH>                  Path to the project root directory (defaults to the current
                                                      directory). Pass '.' for the current directory
    -o, --options-path [OPTIONSPATH]                  Path to an atc-coding-rules-updater.json options file. Defaults
                                                      to the one in --project-path if present
        --unify-versions                              Give every project the highest version of a package instead of
                                                      keeping differences through VersionOverride. This changes what
                                                      restores. (default false)
        --build-file [BUILDFILE]                      Solution (.sln/.slnx) or project file to restore. Required when
                                                      the project path holds more than one
        --build-configuration [BUILDCONFIGURATION]    Configuration to restore with. Release turns NuGet warnings into
                                                      errors, as CI does. (default Release)
        --json                                        Emit a machine-readable JSON result on stdout instead of log output

OPTIONS (cpm migrate only):
        --dry-run                                     Show the migration plan without writing any files. (default false)
        --skip-restore-verification                   Do not run dotnet restore before and after to verify the
                                                      migration. Not recommended. (default false)
        --allow-dirty                                 Allow a dirty or missing git working tree. Without git there is
                                                      no simple way to undo the migration. (default false)
```

The exit code is `0` when the repository was migrated, already migrated, had nothing to migrate, or was only
validated (or `--dry-run` was used), and `1` when the migration is blocked or was rolled back - so
`cpm validate` can gate CI. `--build-file` and `--build-configuration` fall back to `buildFile` /
`buildConfiguration` in `atc-coding-rules-updater.json`.

#### Command <span style="color:yellow">analyzer-providers</span>

Each analyzer provider scrapes its rule catalog from the upstream documentation site. The results
are cached under `%TEMP%/AtcAnalyzerProviderBaseRules`, and are what the temporary-suppression
feature uses to annotate suppressions with a category, title and help link.

```powershell
USAGE:
    atc-coding-rules-updater.exe analyzer-providers [OPTIONS] <COMMAND>

EXAMPLES:
    atc-coding-rules-updater.exe analyzer-providers collect .
    atc-coding-rules-updater.exe analyzer-providers collect -p c:\temp\MyProject
    atc-coding-rules-updater.exe analyzer-providers collect -p c:\temp\MyProject --fetch-mode ReCollect --verbose
    atc-coding-rules-updater.exe analyzer-providers cleanup-cache

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    collect          Collect base rules metadata from all Analyzer providers
    cleanup-cache    Cleanup cache from Analyzer providers
```

Options for `analyzer-providers collect`:

```powershell
    -p, --project-path <PROJECTPATH>              Path to the project root directory
    -o, --options-path [OPTIONSPATH]              Path to an atc-coding-rules-updater.json options file
        --fetch-mode [FETCHMODE]                  Where to read the rule metadata from. Valid values are:
                                                 LocalCache (default), GitHub, ReCollect
        --include-providers [INCLUDEPROVIDERS]    Comma-separated provider names to include
                                                 (e.g. "AsyncFixer,Meziantou.Analyzer"). When set, only these run
        --exclude-providers [EXCLUDEPROVIDERS]    Comma-separated provider names to exclude
                                                 (e.g. "SonarAnalyzer.CSharp"). Applied after --include-providers
        --json                                   Emit a machine-readable JSON summary on stdout
                                                 (Name / Rules.Count / ExceptionMessage per provider). Useful for CI
```

| `--fetch-mode` | Behaviour                                                                       |
|---------------|---------------------------------------------------------------------------------|
| `LocalCache`  | Use the cached snapshot in the temp folder if present; otherwise fall back to GitHub. |
| `GitHub`      | Read the pre-collected JSON from the `atc-coding-rules-updater` repository.       |
| `ReCollect`   | Re-scrape every provider's documentation site, ignoring both caches.              |

#### Command <span style="color:yellow">options-file</span>
```powershell
USAGE:
    atc-coding-rules-updater.exe options-file [OPTIONS] <COMMAND>

EXAMPLES:
    atc-coding-rules-updater.exe options-file create .       (equivalent to 'options-file create -p [CurrentFolder]')
    atc-coding-rules-updater.exe options-file create -p .    (equivalent to 'options-file create -p [CurrentFolder]')
    atc-coding-rules-updater.exe options-file create -p c:\temp\MyProject
    atc-coding-rules-updater.exe options-file create -p c:\temp\MyProject -t DotNet5
    atc-coding-rules-updater.exe options-file validate .     (equivalent to 'options-file validate -p [CurrentFolder]')
    atc-coding-rules-updater.exe options-file validate -p c:\temp\MyProject

OPTIONS:
    -h, --help    Prints help information

COMMANDS:
    create      Create default options file 'atc-coding-rules-updater.json' if it doesn´t exist
    validate    Validate the options file 'atc-coding-rules-updater.json'
```

### Example

Having a project folder in c:\code\MyProject where the .sln file for C# projects exists in the root, run the following command

```powershell
atc-coding-rules-updater run -p c:\code\MyProject --verbose
```

Running the command above produces the following output

```powershell
  ____            _                                      _           _
 |  _ \   _   _  | |   ___   ___     _   _   _ __     __| |   __ _  | |_    ___   _ __
 | |_) | | | | | | |  / _ \ / __|   | | | | | `_ \   / _` |  / _` | | __|  / _ \ | `__|
 |  _ <  | |_| | | | |  __/ \__ \   | |_| | | |_) | | (_| | | (_| | | |_  |  __/ | |
 |_| \_\  \__,_| |_|  \___| |___/    \__,_| | .__/   \__,_|  \__,_|  \__|  \___| |_|
                                            |_|
🐭 Working on EditorConfig files
🟢   root: .editorconfig files merged
🟢   sample: .editorconfig created
🟢   src: .editorconfig created
🟢   test: .editorconfig created
🔨 Working on Directory.Build.props files
🟢   root: Directory.Build.props created
🟢   sample: Directory.Build.props created
🟢   src: Directory.Build.props created
🟢   test: Directory.Build.props created
✅ Done
```

## Options file schema / example

The tool has an optional options parameter, which can be used to control the paths for persisting the .editorconfigs and props files. This can be applied as follows `--options-path 'C:\Temp\atc-coding-rules-updater.json'`

By default the atc-coding-rules-updater will detect projects in the solution and try to fetch the matching project specific rules and supply an .editorconfig in those projects. This mapping can however be over-ruled as seen in [atc-coding-rules-updater.json example 3](#atc-coding-rules-updaterjson-example-3)

### atc-coding-rules-updater.json example 1

```json
{
    "mappings": {
        "sample": {
            "paths": [
                "C:\\Temp\\MyProject\\sample1",
                "C:\\Temp\\MyProject\\sample2"
            ]
        },
        "src": {
            "paths": [
                "C:\\Temp\\MyProject\\src"
            ]
        },
        "test": {
            "paths": [
                "C:\\Temp\\MyProject\\test"
            ]
        }
    }
}
```

### atc-coding-rules-updater.json example 2

```json
{
    "mappings": {
        "src": { "paths": [ "source" ] },
        "test": { "paths": [ "tests" ] }
    }
}
```

### atc-coding-rules-updater.json example 3

In this example a project framework mapping has been added. The entry is mapped using the CsProj fileName and the specific ProjectFrameworkType. Currently the supported values for project framework type are [defined here](/src/Atc.CodingRules.Updater/ProjectFrameworkType.cs)

By specifying this mapping it will over-rule the automatic detection of the project framework type.

```json
{
    "projectTarget": "DotNet10",
    "mappings": {
        "sample": { "paths": [ "sample" ] },
        "src": { "paths": [ "src" ] },
        "test": { "paths": [ "test" ] }
    },
    "projectFrameworkMappings": [
    {
      "name": "HelloWorldWpf",
      "type": "Wpf"
    }
  ]
}
```

### atc-coding-rules-updater.json default

```json
{
  "projectTarget": "DotNet10",
  "useLatestMinorNugetVersion": true,
  "useTemporarySuppressions": false,
  "temporarySuppressionAsExcel": false,
  "analyzerProviderCollectingMode": "LocalCache",
  "mappings": {
    "src": {
      "paths": [
        "src"
      ]
    },
    "test": {
      "paths": [
        "test"
      ]
    }
  },
  "projectFrameworkMappings": []
}
```

**Note:** If there is a `atc-coding-rules-updater.json` file present in the root folder (given by options `--project-path` /  `-p`), then it will automatically be found and used. Other given arguments will then override.

## CLI Tool Usage from PowerShell or Bash

To ensure that the latest version of the CLI tool `atc-coding-rules-updater` is being used, the following methodology can be used:

1) Download the 2 files from `sample` into a project root folder.
2) Modify the `atc-coding-rules-updater.json` to the projects specific needs.
3) Run `atc-coding-rules-updater.ps1` from PowerShell, or `atc-coding-rules-updater.sh` from Bash (copy it from the repository root alongside the `atc-coding-rules-updater.json`).

## Deep dive in what `atc-coding-rules-updater` actual does and doesn't do

The `atc-coding-rules-updater` downloads files from the [atc-coding-rules repository's distribution folder](https://github.com/atc-net/atc-coding-rules/tree/main/distribution).

From here it works with 2 concepts:

* Scaffolding files (`.editorconfig` and `Directory.Build.props`) - if a file does not exist - it creates a copy.
* Updating files (`.editorconfig`) - if a file exist - updates the files first content part and does not touch the second content part.
  * First content part is related to rules above the line `# Custom - Code Analyzers Rules` - and will be updated - herafter known as ATC-part.
  * Second content part is related to rules below the line `# Custom - Code Analyzers Rules` - and will not be touched - herafter known as Customer-part.

### A use case-scenario for coding rules structure setups - Scenario A

In scenario A we have root where `src` and `test` destination is defined as:

```json
{
    "mappings": {
        "src": { "paths": [ "src" ] },
        "test": { "paths": [ "test" ] }
    }
}
```

When the `atc-coding-rules-update` is exceuted first time, the following happens (see legend for explanation):

* ![#70AD47](https://placehold.co/15x15/70AD47/70AD47.png) arrows indicate files created in `root` folder.
* ![#00B0F0](https://placehold.co/15x15/00B0F0/00B0F0.png) arrows indicate files created in `src` folder.
* ![#7030A0](https://placehold.co/15x15/7030A0/7030A0.png) arrows indicate files created in `test` folder.

![Img](docs/scenario-a-first-run.png)

### A use case-scenario for coding rules structure setups - Scenario B

In this scenario we have root where `src` and `test` destination is defined as:

```json
{
    "mappings": {
        "src": { "paths": [
            "MyDemo.Gui",
            "MyDemo.SharedContracts",
            "MyDemo.WebApi"
            ] },
        "test": { "paths": [
            "MyDemo.Gui.Tests",
            "MyDemo.SharedContracts.Tests",
            "MyDemo.WebApi.Tests"
            ] }
    }
}
```

When the `atc-coding-rules-update` is executed first time, the following happens (see legend for explanation):

* ![#70AD47](https://placehold.co/15x15/70AD47/70AD47.png) arrows indicate files created in `root` folder.
* ![#00B0F0](https://placehold.co/15x15/00B0F0/00B0F0.png) arrows indicate files created in `src` folder.
* ![#7030A0](https://placehold.co/15x15/7030A0/7030A0.png) arrows indicate files created in `test` folder.

![Img](docs/scenario-b-first-run.png)

### For both use-case scenarios

For both scenario A and scenario B, when the `atc-coding-rules-update` is executed a second time it will only update `.editorconfig` file. And as previously stated, it will only update the ATC-part of these files.

![Img](docs/scenario-ab-second-run.png)

## Central Package Management (Directory.Packages.props)

The updater supports repositories that use NuGet
[Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management) (CPM),
where every package version lives in one `Directory.Packages.props`. The guiding rule is that **an existing setup is
never broken**: CPM is detected automatically, and a repository is only ever switched to CPM by the explicit
`cpm migrate` command.

### Detection

CPM is on when `ManagePackageVersionsCentrally` is `true` in the root `Directory.Packages.props` or the root
`Directory.Build.props`. Set `"centralPackageManagement": "Disabled"` in `atc-coding-rules-updater.json` (or pass
`--central-package-management Disabled`) to always use the non-CPM behaviour.

| Repository state | What `run` does |
|---|---|
| No CPM | Exactly what it did before. The files it writes and the `--json` output are unchanged; the console shows one tip about `cpm migrate`. |
| CPM enabled | The CPM-aware path described below. |
| `Directory.Packages.props` exists, but CPM is not enabled | Treated as no CPM, with a warning. The updater never turns the flag on. |
| A mixed or broken CPM state | The sanity checks stop the run before anything is written. |

### What `run` does in a CPM repository

* **Missing props files are created without versions.** The distribution's `Label="Code Analyzers"` group becomes
  `GlobalPackageReference` entries in `Directory.Packages.props`, and every other package becomes a versionless
  `PackageReference` plus a `PackageVersion` entry. Metadata such as `PrivateAssets` stays on the reference.
* **Existing props files are never changed.** Only drift is reported, and a package declared as a
  `GlobalPackageReference` counts as present.
* **`Directory.Packages.props` is only changed in two ways:**
  * `--use-latest-minor-nuget-version` bumps the version of the packages the distribution owns - the code analyzers and
    the distribution's test packages - within the same major version. Every other entry, such as a deliberate version
    cap or a transitive security pin, is never touched.
  * Entries that a props file created in the same run needs are appended under an `atc-coding-rules: ...` label.
    An entry that already exists, in any group, is never duplicated.
* Nothing is removed, reordered or reformatted, and `Directory.Packages.props` is never created by `run`.
* When the SDK pinned in `global.json` is older than 7.0.100, which is the first SDK that understands
  `GlobalPackageReference`, the code analyzers are reported and skipped.

The sanity checks (also available through `sanity-check`) report these CPM problems before anything is written. Build
output such as `obj/` is ignored.

| Code | Severity | Meaning |
|---|---|---|
| `CpmVersionOnPackageReference` | Error | A `PackageReference` has a `Version` - restore error NU1008 (a Warning when every such item is conditional, since the condition may exclude it) |
| `CpmMissingPackageVersion` | Error | A versionless `PackageReference` has no `PackageVersion` - restore error NU1010 (a Warning when every such item is conditional) |
| `CpmDuplicateGlobalAndPackageReference` | Warning | A package is both a `GlobalPackageReference` and referenced directly |
| `CpmProjectOptOut` | Warning | A project sets `ManagePackageVersionsCentrally` to `false`; its references are not checked |
| `CpmPackagesPropsWithoutFlag` | Warning | `Directory.Packages.props` exists, but CPM is not enabled |

### What `cpm validate` and `cpm migrate` do

`cpm validate` only plans (step 2) and reports the result; it writes nothing and does not run git or restore.
`cpm migrate` runs every step below.

1. **Checks** that the repository does not use CPM yet and that the git working tree is clean, so `git checkout .` is
   always a complete undo.
2. **Plans** the migration and reports every blocker in one pass. Blockers include floating versions, `Update=`
   items, an existing `VersionOverride`, `packages.config`, conditional references with different versions, and
   versioned references in imported `.props`/`.targets` files other than `Directory.Build.props`/`.targets`.
3. **Restores** the repository once (Release by default) to record what every project resolves. Every project under
   the root must be part of that restore, because `Directory.Packages.props` affects all of them; otherwise the
   migration stops with `ProjectNotCoveredByRestore` and asks for a `--build-file` that includes every project.
4. **Writes** a new `Directory.Packages.props` and removes the versions from every `PackageReference`:
   * When projects use different versions of a package, the highest becomes the central version and the others keep
     theirs through `VersionOverride`, unless `--unify-versions` is used.
   * The root `Code Analyzers` group becomes `GlobalPackageReference` entries.
   * Versions are not bumped, and `CentralPackageTransitivePinningEnabled` is not turned on, because both would
     change what restores.
5. **Restores again and compares.** If restore fails, raises a new warning (for example NU1507 when there are several
   package sources and no package source mapping), or any project gains, loses or changes a package, every file is
   put back byte for byte and the differences are listed. The git working tree is then checked again, and anything
   left over, such as a `packages.lock.json` that restore rewrote, is reported.

Files are written as UTF-8 and keep their byte-order mark when they had one.

Afterwards, review and commit the changes and run `atc-coding-rules-updater run`, which now takes the CPM path.

**Limitations:** only the root `Directory.Packages.props` is read (nested or imported ones are not followed), and a
per-project opt-out from CPM is reported but not handled.

## Temporary suppressions

When starting with ATC rules on an existing project, the general recommendation is to initially suppress all failing rules in your Custom section in order to get back to ✅ state for the project, and then later clean up the code and remove the suppressions one by one in nice clean commits.

The challenge is that this initial suppression list takes a long time to hand-write. In order to circumvent this tedious process, some extra flags have been added to the CLI to allow for auto-generation of these suppression lines. When utilizing these new options, the updater process will run a dotnet build on your project and extract any errors and create suppressions for these along with a count of how many occurences are present. The CLI will also add additional information, e.g: Category, Title, Link to the rule being broken.

The three CLI options for generating temporary suppressions are as follows.

```json
--use-temporary-suppressions      Use temporary suppressions from build - appends to .editorconfig - unless temporarySuppressionPath is set
--temporary-suppression-path      Optional path to temporary suppressions file - if not set .editorconfig file is used
--temporary-suppression-as-excel   Optional - save temporary suppressions file as Excel (.xlsx)
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
dotnet_diagnostic.CA1303.severity = none            # 1 occurrence - Do not pass literals as localized parameters - https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1303
dotnet_diagnostic.CA1707.severity = none            # 1 occurrence - Identifiers should not contain underscores - https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1707
dotnet_diagnostic.CA1801.severity = none            # 1 occurrence - Review unused parameters - https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1801

# SonarAnalyzer.CSharp
dotnet_diagnostic.S1118.severity = none             # 1 occurrence - Category: 'Code Smell' - Utility classes should not have public constructors - https://rules.sonarsource.com/csharp/RSPEC-1118

# StyleCop.Analyzers
dotnet_diagnostic.SA1400.severity = none            # 2 occurrences - Category: 'Maintainability' - Access modifier must be declared - https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1400.md
```

### Build

When using the `--use-temporary-suppressions` option, a `dotnet.exe build` will be executed (up to 10 times depending on the complexity of the solution).
Hence a requirement is that dotnet.exe can be called from the root path.
If there are multiple solution files in the root folder, the `--build-file` option will then be required as a parameter when calling the CLI tool.

```json
--build-file                 Optional path to solution/project file - required when multiple solution files exist in the root path
```

Recognised solution and project files are `.sln`, `.slnx` and `.csproj`. If the root path contains
none of these, the build loop is skipped with a message rather than failing.

#### When a build target blocks the updater

If a target in your build breaks the suppression build — an obfuscator, a signing step, a packaging
step — there are two levers.

The build runs in Release by default, so if the target is Release-only, switching configuration is
usually enough and requires no change on your side:

```powershell
atc-coding-rules-updater run -s . --build-configuration Debug
```

Otherwise, forward an MSBuild property. **MSBuild has no switch to skip a named target** — `-t:`
selects which targets to run at the top level and will not suppress a target hooked with
`AfterTargets`/`BeforeTargets` — so the target has to carry a condition it can respond to:

```xml
<Target Name="Obfuscate" AfterTargets="Build" Condition="'$(SkipObfuscation)' != 'true'">
```

```powershell
atc-coding-rules-updater run -s . --build-property SkipObfuscation=true
```

`--build-property` is repeatable, and both options can be set in `atc-coding-rules-updater.json`
(`buildConfiguration`, `buildProperties`) so they do not have to be retyped.

If the offending target comes from an imported third-party `.targets` file you cannot edit, check
whether it already exposes a disable property — most do.

## How to contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)

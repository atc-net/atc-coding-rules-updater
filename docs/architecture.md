# Architecture overview

This document describes how `atc-coding-rules-updater` is structured and how the `run` command flows from CLI invocation to file changes on disk. It complements the user-facing scenarios in `README.md` and the existing PowerPoint diagrams in this folder.

## Component map

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Atc.CodingRules.Updater.CLI    (entry point, Spectre.Cli command host)   │
│   • Program.cs                       — wires up DI, parses args          │
│   • Commands/RunCommand              — orchestrates a `run`              │
│   • Commands/SanityCheckCommand      — read-only validation              │
│   • Commands/OptionsFile*            — create / validate the json file   │
│   • Commands/AnalyzerProviders*      — collect / cleanup analyzer data   │
│   • ProjectHelper                    — top-level run orchestrator        │
└────────────────┬─────────────────────────────────────────────────────────┘
                 │ depends on
                 ▼
┌──────────────────────────────────────────────────────────────────────────┐
│ Atc.CodingRules.Updater    (library — no console dependency)             │
│   • EditorConfigHelper              — merge .editorconfig                │
│   • DirectoryBuildPropsHelper       — merge Directory.Build.props        │
│   • ProjectSanityCheckHelper        — pre-flight validation              │
│   • DotnetCsProjHelper              — find .csproj files                 │
│   • CodingRulesUpdaterVersionHelper — self-update notice                 │
└────────────────┬───────────────────────────────────┬─────────────────────┘
                 │ depends on                        │
                 ▼                                   ▼
┌──────────────────────────────┐   ┌────────────────────────────────────────┐
│ Atc.CodingRules              │   │ Atc.CodingRules.AnalyzerProviders      │
│  • HttpClientHelper          │   │  • AnalyzerProviderCollector           │
│    (shared HttpClient,       │   │  • AnalyzerProviderBaseRulesHelper     │
│     in-process URL cache,    │   │  • Providers/* (13 scrapers)           │
│     retry/backoff, 404 ok)   │   │     - one per analyzer ecosystem       │
│  • AtcApiNugetClientHelper   │   │     - inherit AnalyzerProviderBase     │
│    (newest version lookup)   │   │  • Snapshot fallback to %TEMP%/...     │
│  • Constants                 │   │                                        │
└──────────────────────────────┘   └────────────────────────────────────────┘
```

## High-level flow of `run`

```
┌────────────┐   ┌────────────────┐   ┌──────────────────────┐   ┌────────────────────┐
│ RunCommand │──▶│ ProjectHelper  │──▶│ EditorConfigHelper   │──▶│ HttpClientHelper   │
│ (CLI)      │   │ .HandleFiles   │   │ .HandleFile          │   │ (download from     │
│            │   │                │   │ DirectoryBuildProps  │   │  atc-coding-rules) │
└────────────┘   └────────┬───────┘   │ Helper.HandleFile    │   └────────────────────┘
                          │           └──────────┬───────────┘
                          │                      │
                          │                      ▼
                          │            ┌────────────────────────┐
                          │            │ Merge logic:           │
                          │            │ - identical → skip     │
                          │            │ - file missing → write │
                          │            │ - else → merge base    │
                          │            │   keep custom section  │
                          │            └────────────────────────┘
                          │
                          ▼
                ┌────────────────────────────────┐  optional, only if
                │ HandleTemporarySuppressions    │  --useTemporarySuppressions
                │  1. dotnet build               │  is set.
                │  2. parse errors → editorcfg   │
                │  3. repeat up to 9 times       │
                │     until no new errors        │
                │  4. warn if didn't converge    │
                └────────────────────────────────┘
```

In **dry-run mode** (`--dry-run`), every write step in `EditorConfigHelper.HandleFile` and `DirectoryBuildPropsHelper.HandleFile` is replaced with a `would create` / `would merge` log line, and the temporary-suppression block is skipped entirely.

## The .editorconfig merge algorithm

The downloaded "git" content and the local "file" content each have two parts:

```
┌─── base section ─────────────────────────────────┐
│ # ATC coding rules                               │
│ # Version / Updated / Distribution headers       │
│ [*.{cs,csx,cake}]                                │
│ dotnet_diagnostic.SA1234.severity = warning      │
│ ...                                              │
├─── custom section(s) ─ everything below ─────────┤
│ ##########################################       │
│ # Custom - Code Analyzers Rules                  │
│ ##########################################       │
│ dotnet_diagnostic.SA1204.severity = none         │
│ ...                                              │
└──────────────────────────────────────────────────┘
```

`EditorConfigHelper.HandleFile` does:

1. **Empty git content?** Skip and warn (don't overwrite).
2. **Files identical?** Skip ("nothing to update").
3. **Local file missing?** Create from git content.
4. **Identical base sections?** Skip — the custom section is the user's, and the rest of git already matches.
5. **Otherwise:** rewrite the base section from git, **preserve the file's custom sections verbatim**, append any custom-section headers that exist only in git but not yet in the file.

The merge logic lives in `EditorConfigHelper.UpdateFile`, `MergeCustomPartsToFileCustomParts`, and `BuildNewContentFile`. Diagnostics with the same key on both sides produce a duplicate-key warning via `LogSeverityDiffs` so users can resolve the conflict by hand.

When `--useTemporarySuppressions` is on, an additional `# ATC temporary suppressions` section is appended via `UpdateRootFileAddCustomAtcAutogeneratedRuleSuppressions` and removed by `UpdateRootFileRemoveCustomAtcAutogeneratedRuleSuppressions`. This section is auto-generated and rewritten on every run.

## The provider scraping flow

Each `AnalyzerProviderBase` subclass (e.g. `MeziantouProvider`, `StyleCopAnalyzersProvider`, `XunitProvider`) implements `ReCollect` to scrape an upstream documentation page (HTML via HtmlAgilityPack, or a JSON blob embedded in GitHub's `react-app.embeddedData`). `CollectBaseRules` decides where the data comes from:

```
                         providerCollectingMode
                                  │
            LocalCache ◄──────────┼──────────► ReCollect
                 │                              │
                 ▼                              │
   ┌──────────────────────┐                     │
   │ ReadFromTempFolder   │  hit                │
   │ (%TEMP%/AtcAnalyzer- │ ────────────► return cached
   │  ProviderBaseRules)  │                     │
   └──────────┬───────────┘ miss                │
              ▼                                 │
              ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼ ▼
   ┌──────────────────────────┐
   │ ReadFromGithub           │  hit  ────► WriteToTempFolder, return
   │ (atc-coding-rules-       │
   │  updater repo snapshot)  │  miss/empty
   └──────────┬───────────────┘
              ▼
   ┌──────────────────────────┐
   │ ReCollect                │  success ──► WriteToTempFolder, return
   │ (scrape live docs)       │
   │                          │  failure / empty result
   └──────────┬───────────────┘
              ▼
   ┌──────────────────────────┐  hit  ────► return cached snapshot, log warning
   │ ReadFromTempFolder       │
   │ (graceful fallback)      │  miss
   └──────────┬───────────────┘
              ▼
       return data with
       ExceptionMessage set
```

The fallback at the bottom (added in this round of work) means a transient HTML structure change at `learn.microsoft.com` or `github.com` no longer kills a `run`; the previous good snapshot is reused with a warning.

`HttpClientHelper.GetAsString` retries up to 3 times with 200ms / 600ms backoff for non-404 errors. 404s short-circuit and return `string.Empty` without caching, so a later run can pick the resource up if it gets published upstream.

## Where things live

| Concern | File |
|---|---|
| CLI argument parsing | `src/Atc.CodingRules.Updater.CLI/Commands/Settings/*` |
| CLI command bodies | `src/Atc.CodingRules.Updater.CLI/Commands/*Command.cs` |
| Top-level `run` orchestrator | `src/Atc.CodingRules.Updater.CLI/ProjectHelper.cs` |
| `OptionsFile` defaults / mappings | `src/Atc.CodingRules.Updater.CLI/Models/Options/*` |
| Editor-config merge | `src/Atc.CodingRules.Updater/EditorConfigHelper.cs` |
| Directory.Build.props merge | `src/Atc.CodingRules.Updater/DirectoryBuildPropsHelper.cs` |
| Pre-flight checks | `src/Atc.CodingRules.Updater/ProjectSanityCheckHelper.cs` |
| HTTP client + cache | `src/Atc.CodingRules/HttpClientHelper.cs` |
| Provider base class | `src/Atc.CodingRules.AnalyzerProviders/Providers/AnalyzerProviderBase.cs` |
| Provider registry | `src/Atc.CodingRules.AnalyzerProviders/AnalyzerProviderCollector.cs` |

## Adding a new analyzer provider

1. Create `Providers/MyAnalyzerProvider.cs` that derives from `AnalyzerProviderBase`.
2. Implement `static string Name => "..."`, `DocumentationLink`, `CreateData`, and `ReCollect`.
3. Add the provider to `AnalyzerProviderCollector.GetAllBaseRuleProviderNames` and `CreateAllProviders`. (Both lists must stay in sync.)
4. Add a `ProvidersTests/MyAnalyzerProviderTests.cs` that asserts at least one well-known rule is present after collection.

The base class handles caching, snapshot fallback, and timing — `ReCollect` only needs to populate `data.Rules` (or set `data.ExceptionMessage` on a structural failure).

## Adding a new project-framework editor-config

1. Add a new value to `ProjectFrameworkType`.
2. Map any `DotnetProjectType` → new value in `ProjectHelper.DetermineProjectFrameworkType`.
3. Publish the matching `.editorconfig` to `atc-coding-rules/distribution/project-frameworks/<lowercase-name>/.editorconfig`. The updater finds it by URL convention.

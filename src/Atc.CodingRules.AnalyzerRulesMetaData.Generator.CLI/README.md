# Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI

Standalone helper that writes a JSON snapshot of every analyzer provider's rule set to disk. Intended as a one-off utility for inspecting the catalog produced by `Atc.CodingRules.AnalyzerProviders`.

## Status: orphan / broken — review for removal

This project is **not part of the solution** (`Atc.CodingRules.Updater.CLI.slnx` does not reference it) and has been drifting out of sync with the `AnalyzerProviders` API:

- Calls `new AnalyzerProviderCollector()` — the constructor now requires an `ILogger`.
- Calls `CollectAllBaseRules()` with no arguments — the method now requires `ProviderCollectingMode` and `bool logWithAnsiConsoleMarkup`.
- Writes to a hardcoded `C:\Code\…` path that only exists on the original author's machine.
- The file already carries `// TODO: Kill this project`.

## Two options for resolving the orphan

1. **Delete the project** — its functionality is fully covered by `atc-coding-rules-updater analyzer-providers collect --json` (added in the recent roadmap work), which streams the same data to stdout as JSON without a hardcoded output path.
2. **Repair and re-include** — wire up an `ILogger` (e.g. via `Microsoft.Extensions.Logging` console provider), fix the `CollectAllBaseRules` call to pass `ProviderCollectingMode.LocalCache` and `false`, parameterise the output path, and add the project to the solution.

Recommendation: option 1, since the JSON output flag covers the same use case from the supported CLI.

## Original purpose (for reference)

```csharp
var analyzerProviders = new AnalyzerProviderCollector();
var apsData = await analyzerProviders.CollectAllBaseRules();
File.WriteAllTextAsync(@"C:\Code\…\AnalyzerRulesMetaData.json", json, Encoding.UTF8);
```

The intent was to materialise the rule catalog (all 13 providers, every analyzer rule with title / category / link) into one JSON file for offline inspection or downstream tooling.

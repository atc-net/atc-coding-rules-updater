namespace Atc.CodingRules.Updater.CLI.Models;

/// <summary>A package reference whose version was raised.</summary>
/// <param name="PackageId">The NuGet package id.</param>
/// <param name="FromVersion">Version declared before the run.</param>
/// <param name="ToVersion">Version the run moved it to.</param>
public sealed record RunPackageBump(
    string PackageId,
    string FromVersion,
    string ToVersion);
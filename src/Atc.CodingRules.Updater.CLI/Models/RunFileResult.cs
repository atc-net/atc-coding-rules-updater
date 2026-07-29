namespace Atc.CodingRules.Updater.CLI.Models;

/// <summary>One rule file the run touched.</summary>
/// <param name="Area">Mapping area — <c>root</c>, <c>src</c>, <c>test</c>, <c>sample</c> or a project-framework name.</param>
/// <param name="File">File name, either <c>.editorconfig</c> or <c>Directory.Build.props</c>.</param>
/// <param name="Path">Absolute path of the local file.</param>
/// <param name="Outcome">What happened to it.</param>
public sealed record RunFileResult(
    string Area,
    string File,
    string Path,
    FileUpdateOutcome Outcome);
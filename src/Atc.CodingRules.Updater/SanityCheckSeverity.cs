namespace Atc.CodingRules.Updater;

/// <summary>
/// Severity of a single <see cref="SanityCheckDiagnostic"/>.
/// </summary>
public enum SanityCheckSeverity
{
    /// <summary>Informational warning the user should fix but which doesn't block a run.</summary>
    Warning,

    /// <summary>Hard error: <see cref="ProjectSanityCheckHelper.CheckFiles"/> with <c>throwIf:true</c> raises this as a <see cref="System.Data.DataException"/>.</summary>
    Error,
}
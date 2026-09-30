namespace Atc.CodingRules.Updater.CentralPackageManagement;

/// <summary>
/// The result of <see cref="CpmContentTransformer.Transform"/>.
/// </summary>
/// <param name="Content">The props content with every version moved out.</param>
/// <param name="RequiredEntries">What <c>Directory.Packages.props</c> must declare for <paramref name="Content"/> to restore.</param>
/// <param name="AnalyzersSkipped"><c>true</c> when the code analyzers were left out because the SDK does not support <c>GlobalPackageReference</c>.</param>
public sealed record CpmTransformResult(
    string Content,
    IReadOnlyList<CpmRequiredEntry> RequiredEntries,
    bool AnalyzersSkipped);
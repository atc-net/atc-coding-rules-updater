namespace Atc.CodingRules.Updater.CLI.Models.Options;

public class OptionsFile
{
    public SupportedProjectTargetType ProjectTarget { get; set; } = SupportedProjectTargetType.DotNet10;

    public bool UseLatestMinorNugetVersion { get; set; } = true;

    public bool UseTemporarySuppressions { get; set; }

    public string? TemporarySuppressionsPath { get; set; }

    public bool TemporarySuppressionAsExcel { get; set; }

    public ProviderCollectingMode AnalyzerProviderCollectingMode { get; set; } = ProviderCollectingMode.LocalCache;

    public bool DryRun { get; set; }

    /// <summary>
    /// Ask the ATC API to bypass its own version cache. Normally left off; the API caches
    /// resolved versions for 12 hours, so this is the escape hatch when a package published
    /// inside that window is being reported as the previous version.
    /// </summary>
    public bool ForceNugetRefresh { get; set; }

    public string? BuildFile { get; set; }

    public OptionsMappings Mappings { get; set; } = new();

    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "OK.")]
    public IList<OptionsProjectFrameworkMapping> ProjectFrameworkMappings { get; set; } = [];

    public override string ToString()
        => $"{nameof(ProjectTarget)}: {ProjectTarget}, {nameof(UseTemporarySuppressions)}: {UseTemporarySuppressions}, {nameof(TemporarySuppressionsPath)}: {TemporarySuppressionsPath}, {nameof(TemporarySuppressionAsExcel)}: {TemporarySuppressionAsExcel}, {nameof(Mappings)}: ({Mappings}), {nameof(ProjectFrameworkMappings)}.Count: {ProjectFrameworkMappings.Count}";
}
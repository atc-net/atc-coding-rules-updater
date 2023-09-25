namespace Atc.CodingRules.Updater.CLI.Models.Options;

public class OptionsFile
{
    public SupportedProjectTargetType ProjectTarget { get; set; } = SupportedProjectTargetType.DotNet8;

    public bool UseLatestMinorNugetVersion { get; set; } = true;

    public bool UseTemporarySuppressions { get; set; }

    public string? TemporarySuppressionsPath { get; set; }

    public bool TemporarySuppressionAsExcel { get; set; }

    public ProviderCollectingMode AnalyzerProviderCollectingMode { get; set; } = ProviderCollectingMode.LocalCache;

    public string? BuildFile { get; set; }

    public OptionsMappings Mappings { get; set; } = new();

    public bool HasMappingsPaths()
        => Mappings.HasMappingsPaths();

    public override string ToString()
        => $"{nameof(ProjectTarget)}: {ProjectTarget}, {nameof(UseTemporarySuppressions)}: {UseTemporarySuppressions}, {nameof(TemporarySuppressionsPath)}: {TemporarySuppressionAsExcel}, {nameof(TemporarySuppressionAsExcel)}: {TemporarySuppressionsPath}, {nameof(Mappings)}: ({Mappings})";
}
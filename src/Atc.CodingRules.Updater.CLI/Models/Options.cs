namespace Atc.CodingRules.Updater.CLI.Models
{
    public class Options
    {
        public OptionsMappings Mappings { get; set; } = new OptionsMappings();

        public bool HasMappingsPaths() =>
            Mappings?.Sample?.Paths?.Count > 0 ||
            Mappings?.Src?.Paths?.Count > 0 ||
            Mappings?.Test?.Paths?.Count > 0;

        public override string ToString()
        {
            return $"{nameof(Mappings)}: ({Mappings})";
        }
    }
}
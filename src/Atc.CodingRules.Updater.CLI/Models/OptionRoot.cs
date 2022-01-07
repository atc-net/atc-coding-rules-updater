namespace Atc.CodingRules.Updater.CLI.Models;

public class OptionRoot
{
    public OptionsMappings Mappings { get; set; } = new ();

    public bool HasMappingsPaths()
        => Mappings.HasMappingsPaths();

    public override string ToString()
        => $"{nameof(Mappings)}: ({Mappings})";
}
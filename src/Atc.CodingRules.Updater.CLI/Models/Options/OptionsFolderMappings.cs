namespace Atc.CodingRules.Updater.CLI.Models.Options;

public class OptionsFolderMappings
{
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "OK.")]
    public IList<string> Paths { get; set; } = [];

    public override string ToString()
        => $"{nameof(Paths)}.Count: {Paths.Count}";
}
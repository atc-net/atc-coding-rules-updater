namespace Atc.CodingRules.AnalyzerProviders.Models;

public class AnalyzerProviderBaseRuleData
{
    public AnalyzerProviderBaseRuleData()
    {
    }

    public AnalyzerProviderBaseRuleData(string name)
    {
        this.Name = name;
    }

    public string Name { get; set; } = string.Empty;

    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "OK.")]
    public ICollection<Rule> Rules { get; set; } = new List<Rule>();

    public string? ExceptionMessage { get; set; }

    public override string ToString() => $"{nameof(Name)}: {Name}, {nameof(Rules)}.Count: {Rules.Count}";
}
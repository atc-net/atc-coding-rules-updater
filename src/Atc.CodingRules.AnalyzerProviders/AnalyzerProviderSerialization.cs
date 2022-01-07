namespace Atc.CodingRules.AnalyzerProviders;

public static class AnalyzerProviderSerialization
{
    public static readonly JsonSerializerOptions JsonOptions = new ()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
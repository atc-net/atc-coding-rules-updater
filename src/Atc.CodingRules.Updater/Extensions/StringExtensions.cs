// ReSharper disable CheckNamespace
// ReSharper disable ReturnTypeCanBeEnumerable.Global
namespace System;

public static class StringExtensions
{
    private static readonly string[] LineBreaks = { "\r\n", "\r", "\n" };

    public static string TrimEndForEmptyLines(
        this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var values = value
            .Split(LineBreaks, StringSplitOptions.None)
            .ToList();

        values.TrimEndForEmptyValues();
        return string.Join(Environment.NewLine, values);
    }

    public static Collection<KeyValueItem> GetKeyValues(
        this string value)
        => string.IsNullOrEmpty(value)
            ? new Collection<KeyValueItem>()
            : value
                .Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries)
                .GetKeyValues();

    public static Collection<KeyValueItem> GetDotnetDiagnosticSeverityKeyValues(
        this string value)
    {
        var data = new Collection<KeyValueItem>();
        foreach (var item in GetKeyValues(value)
                     .Where(x => x.Key.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal) &&
                                 x.Key.Contains(".severity", StringComparison.Ordinal)))
        {
            data.Add(item);
        }

        return data;
    }
}
// ReSharper disable CheckNamespace
// ReSharper disable LoopCanBeConvertedToQuery
namespace System;

public static class StringArrayExtensions
{
    public static Collection<KeyValueItem> GetKeyValues(
        this string[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var data = new Collection<KeyValueItem>();
        if (values.Length == 0)
        {
            return data;
        }

        foreach (var value in values)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith('#'))
            {
                continue;
            }

            var keyValueLine = value.Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (keyValueLine.Length == 2)
            {
                data.Add(new KeyValueItem(keyValueLine[0], keyValueLine[1]));
            }
        }

        return data;
    }

    public static Collection<KeyValueItem> GetDotnetDiagnosticSeverityKeyValues(
        this string[] values)
    {
        var data = new Collection<KeyValueItem>();
        foreach (var item in GetKeyValues(values)
                     .Where(x => x.Key.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal) &&
                                 x.Key.Contains(".severity", StringComparison.Ordinal)))
        {
            data.Add(item);
        }

        return data;
    }
}
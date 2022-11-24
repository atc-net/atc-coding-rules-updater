// ReSharper disable CheckNamespace
namespace System.Collections;

public static class ListExtensions
{
    public static void TrimEndForEmptyValues(
        this IList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var tryAgain = true;
        while (tryAgain)
        {
            if (values.Count == 0)
            {
                tryAgain = false;
            }
            else
            {
                var lastLine = values.Last().Trim();
                if (lastLine.Length == 0)
                {
                    values.RemoveAt(values.Count - 1);
                }
                else
                {
                    tryAgain = false;
                }
            }
        }
    }

    public static string TrimEndForEmptyValuesToString(
        this IList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return string.Empty;
        }

        TrimEndForEmptyValues(values);

        return string.Join(Environment.NewLine, values);
    }
}
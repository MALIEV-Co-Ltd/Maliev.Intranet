namespace Maliev.Intranet.Client.Helpers;

/// <summary>
/// Helper class for formatting currency values in Thai Baht (฿).
/// </summary>
public static class ThaiCurrency
{
    /// <summary>
    /// Formats a decimal value as Thai Baht currency with the ฿ symbol.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="decimals">Number of decimal places (default is 0 for whole numbers).</param>
    /// <returns>Formatted string with ฿ symbol prefix (e.g., "฿1,234.56").</returns>
    public static string Format(decimal amount, int decimals = 0)
    {
        var formatString = decimals > 0 ? $"N{decimals}" : "N0";
        return $"฿{amount.ToString(formatString)}";
    }

    /// <summary>
    /// Formats a nullable decimal value as Thai Baht currency. Returns "-" if null.
    /// </summary>
    /// <param name="amount">The amount to format (can be null).</param>
    /// <param name="decimals">Number of decimal places (default is 0 for whole numbers).</param>
    /// <returns>Formatted string with ฿ symbol prefix or "-" if null.</returns>
    public static string Format(decimal? amount, int decimals = 0)
    {
        if (!amount.HasValue)
            return "-";

        return Format(amount.Value, decimals);
    }
}

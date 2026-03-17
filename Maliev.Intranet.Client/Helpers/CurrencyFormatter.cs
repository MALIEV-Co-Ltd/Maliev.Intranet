using System.Globalization;

namespace Maliev.Intranet.Client.Helpers;

/// <summary>
/// Helper class for formatting currency values with proper symbols.
/// </summary>
public static class CurrencyFormatter
{
    private static readonly Dictionary<string, string> _defaultSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        { "THB", "฿" },
        { "USD", "$" },
        { "EUR", "€" },
        { "GBP", "£" },
        { "JPY", "¥" },
        { "CNY", "¥" },
        { "KRW", "₩" },
        { "INR", "₹" },
        { "SGD", "S$" },
        { "MYR", "RM" },
        { "VND", "₫" },
        { "PHP", "₱" },
        { "IDR", "Rp" },
        { "AUD", "A$" },
        { "CAD", "C$" },
        { "CHF", "CHF" },
    };

    /// <summary>
    /// Formats a decimal value with the specified currency code.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="currencyCode">The ISO 4217 currency code (e.g., THB, USD).</param>
    /// <param name="decimals">Number of decimal places (default is 2).</param>
    /// <returns>Formatted string with currency symbol prefix (e.g., "฿1,234.56").</returns>
    public static string Format(decimal amount, string? currencyCode = null, int decimals = 2)
    {
        var symbol = GetSymbol(currencyCode);
        var formatString = decimals > 0 ? $"N{decimals}" : "N0";
        return $"{symbol}{amount.ToString(formatString)}";
    }

    /// <summary>
    /// Formats a nullable decimal value. Returns "-" if null.
    /// </summary>
    /// <param name="amount">The amount to format (can be null).</param>
    /// <param name="currencyCode">The ISO 4217 currency code.</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Formatted string with currency symbol prefix or "-" if null.</returns>
    public static string Format(decimal? amount, string? currencyCode = null, int decimals = 2)
    {
        if (!amount.HasValue)
            return "-";

        return Format(amount.Value, currencyCode, decimals);
    }

    /// <summary>
    /// Formats a double value with the specified currency code.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="currencyCode">The ISO 4217 currency code.</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Formatted string with currency symbol prefix.</returns>
    public static string Format(double amount, string? currencyCode = null, int decimals = 2)
    {
        return Format((decimal)amount, currencyCode, decimals);
    }

    /// <summary>
    /// Gets the currency symbol for the given currency code.
    /// </summary>
    /// <param name="currencyCode">The ISO 4217 currency code.</param>
    /// <returns>The currency symbol, or the code itself if not found.</returns>
    public static string GetSymbol(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return "฿";

        if (_defaultSymbols.TryGetValue(currencyCode, out var symbol))
            return symbol;

        return currencyCode;
    }

    /// <summary>
    /// Formats the amount using the specified symbol directly.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="symbol">The currency symbol to use.</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Formatted string with the given symbol prefix.</returns>
    public static string FormatWithSymbol(decimal amount, string symbol, int decimals = 2)
    {
        var formatString = decimals > 0 ? $"N{decimals}" : "N0";
        return $"{symbol}{amount.ToString(formatString)}";
    }
}

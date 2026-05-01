using System.Text.RegularExpressions;

namespace Maliev.Intranet.Client.Services;

/// <summary>
/// Converts internal finish/material/process codes to human-readable display text.
/// </summary>
public static partial class FinishDisplay
{
    /// <summary>
    /// Humanizes an underscore-separated code into title-case words.
    /// e.g. "AS_PRINTED" → "As printed", "BEAD_BLAST" → "Bead blast".
    /// </summary>
    public static string Humanize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return string.Empty;
        var words = code.Replace("_", " ").ToLowerInvariant();
        return CapitalizeFirst().Replace(words, m => m.Value.ToUpperInvariant());
    }

    [GeneratedRegex("^.")]
    private static partial Regex CapitalizeFirst();
}

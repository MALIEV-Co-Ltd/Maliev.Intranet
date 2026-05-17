namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// Converts internal manufacturing process codes into employee-facing labels.
/// </summary>
public static class ProcessDisplayHelper
{
    /// <summary>Returns a human-readable label for an internal process code.</summary>
    public static string GetName(string? code, string emptyLabel = "No process")
    {
        if (string.IsNullOrWhiteSpace(code))
            return emptyLabel;

        return Normalize(code) switch
        {
            "FDM" or "FFF" => "FDM",
            "3DPRINTING" => "3D Printing",
            "SLA" => "SLA",
            "SLADLP" => "SLA / DLP",
            "DLP" => "DLP",
            "SLS" or "SLSPA" => "SLS",
            "CNC" or "CNCMILL" or "CNCMILLING" => "CNC Milling",
            "CNCTURN" or "CNCTURNING" => "CNC Turning",
            "SHEETMETAL" => "Sheet Metal",
            "INJECTIONMOLDING" => "Injection Molding",
            "CASTING" => "Casting",
            "DIECASTING" => "Die Casting",
            "EXTRUSION" => "Extrusion",
            "FABRICATION" => "Fabrication",
            _ => HumanizeCode(code),
        };
    }

    private static string Normalize(string value)
    {
        var chars = value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray();
        return new string(chars);
    }

    private static string HumanizeCode(string code)
    {
        var words = code
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(FormatWord);

        var label = string.Join(" ", words);
        return string.IsNullOrWhiteSpace(label) ? code : label;
    }

    private static string FormatWord(string word)
    {
        var upper = word.ToUpperInvariant();
        return upper switch
        {
            "CNC" or "DFM" or "DLP" or "FDM" or "FFF" or "ISO" or "PA" or "SLA" or "SLS" => upper,
            _ => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant(),
        };
    }
}

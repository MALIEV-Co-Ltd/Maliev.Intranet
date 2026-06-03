namespace Maliev.Intranet.Client.Components.Project;

internal static class ProcessCodeNormalizer
{
    public static string? Normalize(string? processCode)
    {
        if (string.IsNullOrWhiteSpace(processCode))
            return null;

        var normalized = processCode.Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToUpperInvariant();

        return normalized switch
        {
            "CNC" or "CNC_MILL" or "CNC_MILLING" or "MILLING" => "CNC_MILL",
            "CNC_TURN" or "CNC_TURNING" or "TURNING" or "LATHE" => "CNC_TURN",
            "CNC_5AXIS" or "CNC_5_AXIS" or "CNC_5AXIS_MILLING" or "CNC_5_AXIS_MILLING" => "CNC_5AXIS",
            "SLA" or "DLP" or "SLA_DLP" => "SLA_DLP",
            "INJECTION" or "INJECTION_MOLDING" or "INJECTION_MOLD" => "INJECTION_MOLD",
            "SHEETMETAL" or "SHEET_METAL_CUTTING" or "SHEET_METAL_BENDING" or "SHEET_METAL_WELDING" => "SHEET_METAL",
            "THREEDSCANNING" or "3D_SCANNING" => "3D_SCANNING",
            _ => normalized,
        };
    }

    public static bool Equals(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
}

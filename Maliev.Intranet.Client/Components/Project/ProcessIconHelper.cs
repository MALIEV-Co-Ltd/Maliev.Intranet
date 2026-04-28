namespace Maliev.Intranet.Client.Components.Project;

/// <summary>
/// Provides inline SVG markup for manufacturing process icons, sized 24×24 with
/// <c>stroke="currentColor"</c> so they inherit the surrounding text colour.
/// </summary>
public static class ProcessIconHelper
{
    private const string SvgWrap = """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">""";
    private const string SvgEnd = "</svg>";

    /// <summary>Returns an inline SVG string for the given manufacturing process code.</summary>
    public static string GetProcessSvg(string? code) => (code?.ToUpperInvariant()) switch
    {
        // FDM / FFF — heated nozzle extruding filament
        "FDM" or "FFF" or "3D_PRINTING" => Nozzle(),

        // SLA / DLP — resin vat with laser/UV
        "SLA" or "SLA_DLP" or "DLP" => ResinVat(),

        // SLS — powder bed with laser
        "SLS" or "SLS_PA" => PowderBed(),

        // CNC milling / turning — spindle with cutting tool
        "CNC" or "CNC_MILL" or "CNC_MILLING" or "CNC_TURN" or "CNC_TURNING" or "CNC-MILLING" or "CNC-TURNING" => Spindle(),

        // Sheet metal — bending brake
        "SHEET_METAL" or "SHEET-METAL" => BendingBrake(),

        // Injection molding — two-plate mold
        "INJECTION_MOLDING" => Mold(),

        // Casting / die casting — crucible pour
        "CASTING" or "DIE_CASTING" or "DIE-CASTING" => Crucible(),

        // Extrusion — die with profile
        "EXTRUSION" => ExtrusionDie(),

        // Fabrication / generic — wrench
        "FABRICATION" => Wrench(),

        // Default — gear
        _ => Gear(),
    };

    private static string Nozzle() =>
        $"""{SvgWrap}<path d="M12 2 L12 8"/><path d="M9 8 L15 8 L14 14 L10 14 Z"/><path d="M10.5 14 L10.5 18"/><path d="M13.5 14 L13.5 18"/><path d="M8 18 L16 18"/><line x1="12" y1="2" x2="12" y2="1"/>{SvgEnd}""";

    private static string ResinVat() =>
        $"""{SvgWrap}<rect x="5" y="14" width="14" height="6" rx="1"/><path d="M8 14 L8 10 Q8 8 10 8 L14 8 Q16 8 16 10 L16 14"/><line x1="12" y1="4" x2="12" y2="8"/><circle cx="12" cy="3" r="1.5"/>{SvgEnd}""";

    private static string PowderBed() =>
        $"""{SvgWrap}<rect x="4" y="12" width="16" height="8" rx="1"/><path d="M6 12 L6 10 Q6 8 8 8 L16 8 Q18 8 18 10 L18 12"/><line x1="12" y1="3" x2="12" y2="7"/><path d="M10 5 L14 5"/>{SvgEnd}""";

    private static string Spindle() =>
        $"""{SvgWrap}<circle cx="12" cy="8" r="4"/><circle cx="12" cy="8" r="1.5"/><line x1="12" y1="12" x2="12" y2="20"/><path d="M8 20 L16 20"/><path d="M10 16 L14 16"/>{SvgEnd}""";

    private static string BendingBrake() =>
        $"""{SvgWrap}<path d="M4 8 L20 8 L20 12"/><path d="M4 8 L4 20"/><path d="M4 20 L12 20"/><path d="M14 14 L20 20"/><path d="M14 20 L20 14"/>{SvgEnd}""";

    private static string Mold() =>
        $"""{SvgWrap}<rect x="3" y="6" width="8" height="12" rx="1"/><rect x="13" y="6" width="8" height="12" rx="1"/><path d="M11 10 L13 10"/><path d="M11 14 L13 14"/><line x1="12" y1="3" x2="12" y2="6"/>{SvgEnd}""";

    private static string Crucible() =>
        $"""{SvgWrap}<path d="M6 8 L6 16 Q6 20 10 20 L14 20 Q18 20 18 16 L18 8"/><line x1="4" y1="8" x2="20" y2="8"/><path d="M12 4 L12 8"/><path d="M9 4 Q9 2 12 2 Q15 2 15 4"/>{SvgEnd}""";

    private static string ExtrusionDie() =>
        $"""{SvgWrap}<rect x="3" y="8" width="6" height="8" rx="1"/><rect x="15" y="8" width="6" height="8" rx="1"/><path d="M9 10 L15 10"/><path d="M9 14 L15 14"/><rect x="10" y="11" width="4" height="2"/>{SvgEnd}""";

    private static string Wrench() =>
        $"""{SvgWrap}<path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/>{SvgEnd}""";

    private static string Gear() =>
        $"""{SvgWrap}<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>{SvgEnd}""";
}

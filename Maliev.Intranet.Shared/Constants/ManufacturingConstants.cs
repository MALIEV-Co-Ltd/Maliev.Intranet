namespace Maliev.Intranet.Shared.Constants;

/// <summary>
/// Provides unique identifiers and lookup functionality for manufacturing processes used within the MALIEV ecosystem.
/// </summary>
public static class ManufacturingProcesses
{
    /// <summary>
    /// Unique identifier for the Fused Deposition Modeling (FDM) 3D printing process.
    /// </summary>
    public static readonly Guid Fdm3DPrinting = new("f3d3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");

    /// <summary>
    /// Unique identifier for the Stereolithography (SLA) 3D printing process.
    /// </summary>
    public static readonly Guid Sla3DPrinting = new("51a3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");

    /// <summary>
    /// Unique identifier for Computer Numerical Control (CNC) machining processes.
    /// </summary>
    public static readonly Guid CncMachining = new("c4c3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");

    /// <summary>
    /// Unique identifier for sheet metal fabrication processes.
    /// </summary>
    public static readonly Guid SheetMetal = new("5ee3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");

    /// <summary>
    /// Unique identifier for injection molding manufacturing processes.
    /// </summary>
    public static readonly Guid InjectionMolding = new("1413d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");

    /// <summary>
    /// Retrieves the display name associated with a specific manufacturing process identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the manufacturing process.</param>
    /// <returns>The human-readable name of the process; otherwise, "Unknown Process" if the identifier is not recognized.</returns>
    public static string GetName(Guid id)
    {
        if (id == Fdm3DPrinting) return "3D Printing (FDM)";
        if (id == Sla3DPrinting) return "3D Printing (SLA)";
        if (id == CncMachining) return "CNC Machining";
        if (id == SheetMetal) return "Sheet Metal Fabrication";
        if (id == InjectionMolding) return "Injection Molding";
        return "Unknown Process";
    }
}

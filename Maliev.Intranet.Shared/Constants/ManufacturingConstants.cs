namespace Maliev.Intranet.Shared.Constants;

public static class ManufacturingProcesses
{
    public static readonly Guid Fdm3DPrinting = new("f3d3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");
    public static readonly Guid Sla3DPrinting = new("51a3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");
    public static readonly Guid CncMachining = new("c4c3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");
    public static readonly Guid SheetMetal = new("5ee3d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");
    public static readonly Guid InjectionMolding = new("1413d3d3-3d3d-3d3d-3d3d-3d3d3d3d3d3d");

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

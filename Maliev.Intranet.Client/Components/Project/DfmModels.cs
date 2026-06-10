namespace Maliev.Intranet.Client.Components.Project;

/// <summary>Represents a single DFM (Design for Manufacturability) issue to display in the overlay panel.</summary>
/// <param name="Icon">Material icon string for this issue type.</param>
/// <param name="Title">Short title describing the issue.</param>
/// <param name="Description">Detailed description of the issue.</param>
/// <param name="Severity">Severity level: Warning or Error.</param>
/// <param name="Category">Issue category key (e.g. "thin_wall") matching the overlay GLB key "{PROCESS}__{category}".</param>
/// <param name="OverlayKey">Full overlay key "{PROCESS}__{category}" used to load and toggle the overlay GLB.</param>
/// <param name="OverlayUrl">Signed GCS URL to the overlay GLB, or null if none was generated.</param>
/// <param name="Source">Source of the DFM analysis: "local" (browser advisory) or "server" (GeometryService).</param>
/// <param name="FaceIndices">Triangle face indices from the browser-local DFM runtime, used to build the overlay locally when no server overlay GLB exists.</param>
public record DfmIssue(
    string Icon,
    string Title,
    string Description,
    DfmIssueSeverity Severity,
    string? Category = null,
    string? OverlayKey = null,
    string? OverlayUrl = null,
    string? Source = null,
    IReadOnlyList<int>? FaceIndices = null);

/// <summary>Severity level of a DFM issue.</summary>
public enum DfmIssueSeverity
{
    /// <summary>A warning — the part can be manufactured but may have quality or cost implications.</summary>
    Warning,
    /// <summary>An error — the part cannot be manufactured as-is.</summary>
    Error
}

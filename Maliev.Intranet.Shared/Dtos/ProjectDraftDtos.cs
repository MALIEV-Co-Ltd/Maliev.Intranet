using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Shared;

/// <summary>
/// Persisted draft state for the Project New page, stored in sessionStorage.
/// Enables seamless resume after JWT re-authentication without losing work.
/// </summary>
public sealed class DraftProjectState
{
    /// <summary>The temporary project identifier used for file uploads.</summary>
    public Guid TempProjectId { get; set; }

    /// <summary>The project title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The project description.</summary>
    public string? Description { get; set; }

    /// <summary>The selected customer's unique identifier.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>The selected customer's display name.</summary>
    public string? CustomerName { get; set; }

    /// <summary>The selected customer's company name.</summary>
    public string? CustomerCompanyName { get; set; }

    /// <summary>The selected customer's email address.</summary>
    public string? CustomerEmail { get; set; }

    /// <summary>The selected customer's mobile phone number.</summary>
    public string? CustomerMobile { get; set; }

    /// <summary>The selected customer's landline phone number.</summary>
    public string? CustomerLandline { get; set; }

    /// <summary>The selected customer's company phone number.</summary>
    public string? CustomerCompanyPhone { get; set; }

    /// <summary>The selected lead time option code.</summary>
    public string SelectedLeadTimeCode { get; set; } = "STANDARD";

    /// <summary>Timestamp of the last modification to this draft.</summary>
    public DateTime LastModified { get; set; }

    /// <summary>The list of parts associated with this draft project.</summary>
    public List<DraftPartState> Parts { get; set; } = [];
}

/// <summary>
/// Persisted state for a single uploaded part within a draft project.
/// </summary>
public sealed class DraftPartState
{
    /// <summary>The unique file identifier assigned by the UploadService.</summary>
    public Guid FileId { get; set; }

    /// <summary>The storage path of the uploaded file.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>The original file name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The ordered quantity for this part.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>The manufacturing process code (e.g. "FDM", "CNC").</summary>
    public string? ProcessCode { get; set; }

    /// <summary>The manufacturing process unique identifier.</summary>
    public Guid? ProcessId { get; set; }

    /// <summary>The material code.</summary>
    public string? MaterialCode { get; set; }

    /// <summary>The material unique identifier.</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>The surface finish code.</summary>
    public string? SurfaceFinishCode { get; set; }

    /// <summary>The surface finish unique identifier.</summary>
    public Guid? SurfaceFinishId { get; set; }

    /// <summary>The tolerance code.</summary>
    public string? ToleranceCode { get; set; }

    /// <summary>The tolerance unique identifier.</summary>
    public Guid? ToleranceId { get; set; }

    /// <summary>Part-specific notes or instructions.</summary>
    public string? PartNotes { get; set; }

    /// <summary>True when the user has acknowledged all DFM warnings.</summary>
    public bool DfmAcknowledged { get; set; }

    /// <summary>Additional process-specific configuration key-value pairs.</summary>
    public Dictionary<string, string> ProcessConfig { get; set; } = new();

    /// <summary>The part volume in cubic millimeters, extracted during file analysis.</summary>
    public double? VolumeMm3 { get; set; }

    /// <summary>The bounding box dimensions of the part.</summary>
    public FileAnalysisDimensionsDto? Dimensions { get; set; }

    /// <summary>Whether the part mesh is manifold (watertight). Null until analysis completes.</summary>
    public bool? IsManifold { get; set; }

    /// <summary>Signed URL to the small (~256px) isometric thumbnail. Restored to avoid re-fetching on reload.</summary>
    public string? ThumbnailSmallUrl { get; set; }

    /// <summary>Signed URL to the large (~1200px) isometric thumbnail. Restored to avoid re-fetching on reload.</summary>
    public string? ThumbnailLargeUrl { get; set; }

    /// <summary>GCS storage path of the GLB artifact. Non-null when analysis produced a 3D model.</summary>
    public string? GlbStoragePath { get; set; }
}

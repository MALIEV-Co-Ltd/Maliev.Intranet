using System.Collections.ObjectModel;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Response from quality check (Phase 1 of two-phase DFM analysis)
/// </summary>
public class QualityCheckResponse
{
    /// <summary>
    /// Unique identifier for the upload
    /// </summary>
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// Status of the quality check ("quality_check_complete", "error")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Quality metrics from the check
    /// </summary>
    public QualityMetrics Quality { get; set; } = new();

    /// <summary>
    /// Whether the file is ready for process selection
    /// </summary>
    public bool ReadyForProcessSelection { get; set; }
}

/// <summary>
/// Quality metrics from quality check
/// </summary>
public class QualityMetrics
{
    /// <summary>
    /// Whether the mesh is manifold (watertight)
    /// </summary>
    public bool IsManifold { get; set; }

    /// <summary>
    /// Whether the mesh is empty (no faces)
    /// </summary>
    public bool IsEmpty { get; set; }

    /// <summary>
    /// Number of faces in the mesh
    /// </summary>
    public int FaceCount { get; set; }

    /// <summary>
    /// Number of vertices in the mesh
    /// </summary>
    public int VertexCount { get; set; }

    /// <summary>
    /// Volume in cubic millimeters
    /// </summary>
    public double VolumeMm3 { get; set; }

    /// <summary>
    /// Surface area in square millimeters
    /// </summary>
    public double SurfaceAreaMm2 { get; set; }

    /// <summary>
    /// Bounding box dimensions
    /// </summary>
    public BoundingBox BoundingBox { get; set; } = new();

    /// <summary>
    /// Whether the file can be previewed
    /// </summary>
    public bool CanPreview { get; set; }

    /// <summary>
    /// Complexity classification ("simple", "medium", "complex")
    /// </summary>
    public string Complexity { get; set; } = string.Empty;

    /// <summary>
    /// Number of bodies detected
    /// </summary>
    public int BodyCount { get; set; }

    /// <summary>
    /// Optional B-Rep face count (if CAD file provided)
    /// </summary>
    public int? BrepFaceCount { get; set; }
}

/// <summary>
/// Bounding box dimensions
/// </summary>
public class BoundingBox
{
    /// <summary>
    /// X dimension (width) in millimeters
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y dimension (depth) in millimeters
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Z dimension (height) in millimeters
    /// </summary>
    public double Z { get; set; }
}

/// <summary>
/// Response from process-specific DFM analysis (Phase 2 of two-phase DFM analysis)
/// </summary>
public class DfmAnalysisResponse
{
    /// <summary>
    /// Unique identifier for the upload
    /// </summary>
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturing process code analyzed (e.g., "FDM", "SLA", "CNC_MILL")
    /// </summary>
    public string ProcessCode { get; set; } = string.Empty;

    /// <summary>
    /// Status of the analysis ("analysis_complete", "timeout", "error")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// DFM analysis report
    /// </summary>
    public DfmReport DfmReport { get; set; } = new();
}

/// <summary>
/// DFM analysis report for a specific manufacturing process
/// </summary>
public class DfmReport
{
    /// <summary>
    /// Type of report (matches process code)
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// List of DFM issues found
    /// </summary>
    public List<DfmIssue> Issues { get; set; } = new();

    /// <summary>
    /// Analysis time in seconds
    /// </summary>
    public double AnalysisTimeSeconds { get; set; }

    /// <summary>
    /// Number of thin wall issues (printing processes)
    /// </summary>
    public int? ThinWallCount { get; set; }

    /// <summary>
    /// Number of overhang faces (printing processes)
    /// </summary>
    public int? OverhangFaceCount { get; set; }

    /// <summary>
    /// Whether support is required (printing processes)
    /// </summary>
    public bool? SupportRequired { get; set; }

    /// <summary>
    /// Estimated support volume in cubic centimeters (printing processes)
    /// </summary>
    public double? EstimatedSupportVolumeCm3 { get; set; }
}

/// <summary>
/// Individual DFM issue detected
/// </summary>
public class DfmIssue
{
    /// <summary>
    /// Category of issue (e.g., "thin_wall", "overhang", "hole")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Severity level ("error", "warning", "info")
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Title of the issue
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the issue
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Actual value measured
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Threshold value that was exceeded
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Face indices where issue occurs (for visualization)
    /// </summary>
    public List<int> FaceIndices { get; set; } = new();

    /// <summary>
    /// Centroid coordinates [x, y, z] in millimeters (for visualization)
    /// </summary>
    public List<double> Centroid { get; set; } = new();
}

/// <summary>
/// Request payload for lazy DFM analysis (on-demand).
/// </summary>
public class GeometryAnalysisRequest
{
    /// <summary>
    /// GCS storage path of the uploaded file (for cache-miss recovery)
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>
    /// Signed download URL for the file (for cache-miss recovery)
    /// </summary>
    public string? DownloadUrl { get; set; }
}

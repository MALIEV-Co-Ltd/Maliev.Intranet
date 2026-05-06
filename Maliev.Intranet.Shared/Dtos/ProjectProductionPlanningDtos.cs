namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Production planning data for a project detail page.
/// </summary>
public sealed class ProjectProductionPlanDto
{
    /// <summary>Gets or sets the project identifier.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the parts available for production planning.</summary>
    public List<ProjectProductionPartPlanDto> Parts { get; set; } = [];

    /// <summary>Gets or sets the machine schedule board for the project planning tab.</summary>
    public ProductionScheduleBoardDto? ScheduleBoard { get; set; }
}

/// <summary>
/// Production planning data for a quoted project part.
/// </summary>
public sealed class ProjectProductionPartPlanDto
{
    /// <summary>Gets or sets the project part identifier.</summary>
    public Guid PartId { get; set; }

    /// <summary>Gets or sets the source file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the part thumbnail URL.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets the formatted dimensions.</summary>
    public string? Dimensions { get; set; }

    /// <summary>Gets or sets the manufacturing process type.</summary>
    public string? ProcessType { get; set; }

    /// <summary>Gets or sets the material display name.</summary>
    public string? MaterialName { get; set; }

    /// <summary>Gets or sets the part configuration summary.</summary>
    public string? Configuration { get; set; }

    /// <summary>Gets or sets the quoted quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the DFM status label.</summary>
    public string DfmStatus { get; set; } = string.Empty;

    /// <summary>Gets or sets the routing recommendation for the part.</summary>
    public ProductionRoutingDto? Routing { get; set; }

    /// <summary>Gets or sets the active planning hold, if any.</summary>
    public ProductionPlanningHoldDto? ActiveHold { get; set; }

    /// <summary>Gets or sets the converted or downstream job identifier, if available.</summary>
    public Guid? JobId { get; set; }

    /// <summary>Gets or sets the downstream job status, if available.</summary>
    public string? JobStatus { get; set; }

    /// <summary>Gets or sets the assigned machine name, if available.</summary>
    public string? MachineName { get; set; }

    /// <summary>Gets or sets a value indicating whether a planning hold can be created.</summary>
    public bool CanCreateHold { get; set; }

    /// <summary>Gets or sets the reason hold creation is blocked.</summary>
    public string? HoldBlockReason { get; set; }
}

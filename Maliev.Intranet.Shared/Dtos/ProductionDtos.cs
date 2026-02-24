using System.ComponentModel.DataAnnotations;
using Maliev.Intranet.Shared.Enums;

namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// Represents a manufacturing job.
/// </summary>
public class JobDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    
    /// <summary>Gets or sets the associated order ID.</summary>
    public Guid OrderId { get; set; }
    
    /// <summary>Gets or sets the material ID required for the job.</summary>
    public Guid MaterialId { get; set; }
    
    /// <summary>Gets or sets the volume in cubic centimeters.</summary>
    public decimal VolumeCm3 { get; set; }
    
    /// <summary>Gets or sets the manufacturing technology (e.g., FDM, SLA, CNC).</summary>
    public string Technology { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the current status of the job.</summary>
    public JobStatus Status { get; set; }
    
    /// <summary>Gets or sets the machine assigned to this job.</summary>
    public string? AssignedMachineId { get; set; }
    
    /// <summary>Gets or sets the priority level.</summary>
    public int Priority { get; set; }
    
    /// <summary>Gets or sets the timestamp when the job was started.</summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>Gets or sets the timestamp when the job was completed.</summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for jobs in a Kanban board view.
/// </summary>
public class KanbanJobDto
{
    /// <summary>Gets or sets the job ID.</summary>
    public Guid Id { get; set; }
    
    /// <summary>Gets or sets the technology.</summary>
    public string Technology { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the machine ID.</summary>
    public string? MachineId { get; set; }
    
    /// <summary>Gets or sets the volume.</summary>
    public decimal Volume { get; set; }
    
    /// <summary>Gets or sets the priority.</summary>
    public int Priority { get; set; }
}

/// <summary>
/// Response model for the Kanban board.
/// </summary>
public class KanbanResponse
{
    /// <summary>Jobs in Pending status.</summary>
    public List<KanbanJobDto> Pending { get; set; } = [];
    
    /// <summary>Jobs in Queued status.</summary>
    public List<KanbanJobDto> Queued { get; set; } = [];
    
    /// <summary>Jobs in Progress status.</summary>
    public List<KanbanJobDto> InProgress { get; set; } = [];
    
    /// <summary>Jobs in Finishing status.</summary>
    public List<KanbanJobDto> Finishing { get; set; } = [];
    
    /// <summary>Jobs in Completed status.</summary>
    public List<KanbanJobDto> Completed { get; set; } = [];
    
    /// <summary>Jobs in Cancelled status.</summary>
    public List<KanbanJobDto> Cancelled { get; set; } = [];
}

/// <summary>
/// Request payload for queuing a job.
/// </summary>
public class QueueJobRequest
{
    /// <summary>The ID of the machine to assign the job to.</summary>
    [Required]
    public string MachineId { get; set; } = string.Empty;
}

/// <summary>
/// Request payload for cancelling a job.
/// </summary>
public class CancelJobRequest
{
    /// <summary>The reason for cancellation.</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents a batch of material in inventory.
/// </summary>
public class InventoryBatchDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    
    /// <summary>Gets or sets the material ID.</summary>
    public Guid MaterialId { get; set; }
    
    /// <summary>Gets or sets the initial weight in grams.</summary>
    public decimal InitialWeightGrams { get; set; }
    
    /// <summary>Gets or sets the remaining weight in grams.</summary>
    public decimal RemainingWeightGrams { get; set; }
    
    /// <summary>Gets or sets the batch status.</summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the location in the workshop.</summary>
    public string Location { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the timestamp when received.</summary>
    public DateTime ReceivedAt { get; set; }
}

/// <summary>
/// Request for creating a new inventory batch.
/// </summary>
public class CreateInventoryBatchRequest
{
    /// <summary>The material ID.</summary>
    [Required]
    public Guid MaterialId { get; set; }
    
    /// <summary>The initial weight in grams.</summary>
    [Range(0.01, 100000)]
    public decimal InitialWeightGrams { get; set; }
    
    /// <summary>The physical location.</summary>
    [Required]
    public string Location { get; set; } = string.Empty;
    
    /// <summary>Optional low stock threshold.</summary>
    public decimal? LowStockThresholdGrams { get; set; }
}

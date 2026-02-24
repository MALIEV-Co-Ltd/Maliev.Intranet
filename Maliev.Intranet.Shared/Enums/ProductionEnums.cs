namespace Maliev.Intranet.Shared.Enums;

/// <summary>
/// Represents the status of a manufacturing job.
/// </summary>
public enum JobStatus
{
    /// <summary>Job is created but not yet queued.</summary>
    Pending = 0,
    
    /// <summary>Job is queued on a machine.</summary>
    Queued = 1,
    
    /// <summary>Job is currently in production.</summary>
    InProgress = 2,
    
    /// <summary>Job is in post-processing stage.</summary>
    Finishing = 3,
    
    /// <summary>Job is completed successfully.</summary>
    Completed = 4,
    
    /// <summary>Job has been cancelled.</summary>
    Cancelled = 5
}

/// <summary>
/// Represents the status of an inventory batch.
/// </summary>
public enum BatchStatus
{
    /// <summary>Batch is active and available for use.</summary>
    Active = 0,
    
    /// <summary>Batch has been fully consumed.</summary>
    Depleted = 1,
    
    /// <summary>Batch is on hold or reserved.</summary>
    OnHold = 2,
    
    /// <summary>Batch has been discarded or expired.</summary>
    Discarded = 3
}

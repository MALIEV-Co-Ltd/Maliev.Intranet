namespace Maliev.Intranet.Shared.Dtos;

/// <summary>
/// WASM-facing response returned after project files are migrated from temporary storage to customer storage.
/// </summary>
public sealed record BffMigrateProjectResponseDto
{
    /// <summary>Gets whether the migration only reported planned changes.</summary>
    public bool DryRun { get; init; }

    /// <summary>Gets the number of UploadService files evaluated for migration.</summary>
    public int TotalEvaluated { get; init; }

    /// <summary>Gets the number of UploadService files migrated successfully.</summary>
    public int TotalMigrated { get; init; }

    /// <summary>Gets migrated file path mappings and their reconciled analysis status, when available.</summary>
    public List<BffMigratedProjectFileDto> MigratedFiles { get; init; } = [];

    /// <summary>Gets non-fatal migration errors reported by UploadService or the BFF reconciliation step.</summary>
    public List<string> Errors { get; init; } = [];
}

/// <summary>
/// WASM-facing migration result for a single project file.
/// </summary>
public sealed record BffMigratedProjectFileDto
{
    /// <summary>Gets the UploadService file identifier for the migrated source file.</summary>
    public required string FileId { get; init; }

    /// <summary>Gets the original temporary storage path.</summary>
    public required string OldPath { get; init; }

    /// <summary>Gets the new customer-scoped storage path.</summary>
    public required string NewPath { get; init; }

    /// <summary>Gets the reconciled analysis status keyed by <see cref="NewPath"/>, when one exists.</summary>
    public FileAnalysisStatusDto? Status { get; init; }
}

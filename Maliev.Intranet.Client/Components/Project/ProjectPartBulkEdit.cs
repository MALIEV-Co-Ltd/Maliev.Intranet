using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Client.Components.Project;

/// <summary>Explicit field set used to bulk-edit project part configuration without overwriting omitted fields.</summary>
public sealed class PartConfigurationBulkPatch
{
    /// <summary>True when the process field should be applied.</summary>
    public bool IncludeProcess { get; set; }

    /// <summary>The process value to apply when <see cref="IncludeProcess"/> is true.</summary>
    public ProcessDto? Process { get; set; }

    /// <summary>True when the material field should be applied.</summary>
    public bool IncludeMaterial { get; set; }

    /// <summary>The material value to apply when <see cref="IncludeMaterial"/> is true.</summary>
    public CatalogMaterialDto? Material { get; set; }

    /// <summary>True when the surface finish field should be applied.</summary>
    public bool IncludeFinish { get; set; }

    /// <summary>The surface finish value to apply when <see cref="IncludeFinish"/> is true.</summary>
    public CatalogSurfaceFinishDto? Finish { get; set; }

    /// <summary>True when the tolerance field should be applied.</summary>
    public bool IncludeTolerance { get; set; }

    /// <summary>The tolerance value to apply when <see cref="IncludeTolerance"/> is true.</summary>
    public CatalogToleranceDto? Tolerance { get; set; }

    /// <summary>True when the quantity field should be applied.</summary>
    public bool IncludeQuantity { get; set; }

    /// <summary>The quantity value to apply when <see cref="IncludeQuantity"/> is true.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>True when the inspection field should be applied.</summary>
    public bool IncludeInspection { get; set; }

    /// <summary>The inspection level to apply when <see cref="IncludeInspection"/> is true.</summary>
    public InspectionLevel InspectionLevel { get; set; } = InspectionLevel.Standard;

    /// <summary>True when the surface roughness field should be applied.</summary>
    public bool IncludeRoughness { get; set; }

    /// <summary>The roughness code to apply when <see cref="IncludeRoughness"/> is true.</summary>
    public string? RoughnessCode { get; set; }

    /// <summary>True when the threaded-holes flag should be applied.</summary>
    public bool IncludeThreadedHoles { get; set; }

    /// <summary>The threaded-holes value to apply when <see cref="IncludeThreadedHoles"/> is true.</summary>
    public bool HasThreadedHoles { get; set; }

    /// <summary>True when the inserts flag should be applied.</summary>
    public bool IncludeInserts { get; set; }

    /// <summary>The inserts value to apply when <see cref="IncludeInserts"/> is true.</summary>
    public bool HasInserts { get; set; }

    /// <summary>True when the bag-and-tag flag should be applied.</summary>
    public bool IncludeBagAndTag { get; set; }

    /// <summary>The bag-and-tag value to apply when <see cref="IncludeBagAndTag"/> is true.</summary>
    public bool BagAndTag { get; set; } = true;

    /// <summary>True when part notes should be applied.</summary>
    public bool IncludePartNotes { get; set; }

    /// <summary>The part notes value to apply when <see cref="IncludePartNotes"/> is true.</summary>
    public string? PartNotes { get; set; }

    /// <summary>True when dynamic process option values should be applied.</summary>
    public bool IncludeProcessOptions { get; set; }

    /// <summary>Dynamic process option values to apply when <see cref="IncludeProcessOptions"/> is true.</summary>
    public Dictionary<string, string?> ProcessOptionValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Result of applying a bulk configuration patch to a single part.</summary>
/// <param name="AppliedFields">Names of fields that were applied.</param>
/// <param name="SkippedFields">Names of fields that were skipped because the target part was incompatible.</param>
public sealed record PartBulkApplyResult(IReadOnlyList<string> AppliedFields, IReadOnlyList<string> SkippedFields);

/// <summary>Applies explicit bulk-edit patches to <see cref="PartViewModel"/> instances.</summary>
public static class ProjectPartBulkEdit
{
    /// <summary>Applies a process change and clears process-dependent selections.</summary>
    /// <param name="part">The part to mutate.</param>
    /// <param name="process">The target process, or null to clear the process.</param>
    /// <returns>True when the process value changed.</returns>
    public static bool ApplyProcess(PartViewModel part, ProcessDto? process)
    {
        ArgumentNullException.ThrowIfNull(part);

        var nextId = process?.Id;
        var nextCode = process?.Code;
        if (part.ProcessId == nextId && string.Equals(part.ProcessCode, nextCode, StringComparison.OrdinalIgnoreCase))
            return false;

        part.ProcessId = nextId;
        part.ProcessCode = nextCode;
        ClearProcessDependentConfiguration(part);
        ResetDfmForProcessChange(part);
        return true;
    }

    /// <summary>Applies an explicit bulk patch to a part and reports compatible and skipped fields.</summary>
    /// <param name="part">The part to mutate.</param>
    /// <param name="patch">The explicit patch to apply.</param>
    /// <returns>Names of fields that were applied or skipped.</returns>
    public static PartBulkApplyResult ApplyPatch(PartViewModel part, PartConfigurationBulkPatch patch)
    {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(patch);

        var applied = new List<string>();
        var skipped = new List<string>();

        if (patch.IncludeProcess)
        {
            if (patch.Process is null)
                skipped.Add("Process");
            else
            {
                ApplyProcess(part, patch.Process);
                applied.Add("Process");
            }
        }

        if (patch.IncludeMaterial)
        {
            if (patch.Material is not null && Contains(part.AvailableMaterials, patch.Material.Id))
            {
                part.MaterialId = patch.Material.Id;
                part.MaterialCode = patch.Material.Code;
                applied.Add("Material");
            }
            else
            {
                skipped.Add("Material");
            }
        }

        if (patch.IncludeFinish)
        {
            if (patch.Finish is not null && Contains(part.AvailableFinishes, patch.Finish.Id))
            {
                part.FinishId = patch.Finish.Id;
                part.FinishCode = patch.Finish.Code;
                applied.Add("Finish");
            }
            else
            {
                skipped.Add("Finish");
            }
        }

        if (patch.IncludeTolerance)
        {
            if (patch.Tolerance is not null && Contains(part.AvailableTolerances, patch.Tolerance.Id))
            {
                part.ToleranceId = patch.Tolerance.Id;
                part.ToleranceCode = patch.Tolerance.Code;
                applied.Add("Tolerance");
            }
            else
            {
                skipped.Add("Tolerance");
            }
        }

        if (patch.IncludeQuantity)
        {
            part.Quantity = Math.Max(1, patch.Quantity);
            applied.Add("Quantity");
        }

        if (patch.IncludeInspection)
        {
            part.InspectionLevel = patch.InspectionLevel;
            applied.Add("Inspection");
        }

        if (patch.IncludeRoughness)
        {
            part.RoughnessCode = patch.RoughnessCode;
            applied.Add("Roughness");
        }

        if (patch.IncludeThreadedHoles)
        {
            part.HasThreadedHoles = patch.HasThreadedHoles;
            if (!patch.HasThreadedHoles)
            {
                part.ThreadedHoleSpec = null;
                part.ThreadedHoleCount = 0;
            }
            applied.Add("Tapped holes");
        }

        if (patch.IncludeInserts)
        {
            part.HasInserts = patch.HasInserts;
            if (!patch.HasInserts)
            {
                part.InsertType = InsertType.None;
                part.InsertCount = 0;
            }
            applied.Add("Thread inserts");
        }

        if (patch.IncludeBagAndTag)
        {
            part.BagAndTag = patch.BagAndTag;
            applied.Add("Bag and tag");
        }

        if (patch.IncludePartNotes)
        {
            part.PartNotes = patch.PartNotes;
            applied.Add("Notes");
        }

        if (patch.IncludeProcessOptions)
        {
            ApplyProcessOptions(part, patch, applied, skipped);
        }

        return new PartBulkApplyResult(applied, skipped);
    }

    private static bool Contains<T>(IEnumerable<T> items, Guid id)
        where T : notnull =>
        items.Any(item => item switch
        {
            CatalogMaterialDto material => material.Id == id,
            CatalogSurfaceFinishDto finish => finish.Id == id,
            CatalogToleranceDto tolerance => tolerance.Id == id,
            _ => false,
        });

    private static void ApplyProcessOptions(
        PartViewModel part,
        PartConfigurationBulkPatch patch,
        List<string> applied,
        List<string> skipped)
    {
        foreach (var (key, value) in patch.ProcessOptionValues)
        {
            if (part.AvailableProcessOptions.Count > 0
                && part.AvailableProcessOptions.All(option => !string.Equals(option.ConfigKey, key, StringComparison.OrdinalIgnoreCase)))
            {
                skipped.Add($"Option:{key}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
                part.ProcessOptionValues.Remove(key);
            else
                part.ProcessOptionValues[key] = value;

            applied.Add($"Option:{key}");
        }
    }

    private static void ClearProcessDependentConfiguration(PartViewModel part)
    {
        part.MaterialId = null;
        part.MaterialCode = null;
        part.FinishId = null;
        part.FinishCode = null;
        part.ToleranceId = null;
        part.ToleranceCode = null;
        part.AvailableMaterials = [];
        part.AvailableFinishes = [];
        part.AvailableTolerances = [];
        part.AvailableProcessOptions = [];
        part.ProcessOptionValues = new Dictionary<string, string?>();
        part.EstimatedUnitPrice = null;
        part.EstimatedTotalAmount = null;
        part.EstimatedBaseUnitPrice = null;
        part.EstimatedDiscountedUnitPriceBeforeFinish = null;
        part.FinishPricingBaseUnitPrice = null;
        part.FinishAdditionalUnitCost = null;
        part.EstimatedLeadTimeDays = 0;
        part.PricingFailed = false;
        part.ProductionRouting = null;
    }

    private static void ResetDfmForProcessChange(PartViewModel part)
    {
        part.DfmReport = null;
        part.DfmAllClearNotified = false;
        part.DfmAnalysisTimedOut = false;
        part.AnalysisErrorCode = null;
        part.LocalDfmRuntimeRunningProcessCode = null;
        part.LocalDfmRuntimeStartedAtUtc = null;
        part.LocalDfmRuntimeInputByteCount = null;
        part.LocalDfmRuntimeInputTriangleCount = null;
        part.LocalDfmRuntimeTerminalProcessCode = null;
        part.LocalDfmRuntimeTerminalReason = null;
    }
}

using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Client.Components;

public class ProjectPartBulkEditTests
{
    [Fact]
    public void ApplyProcess_WhenProcessChanges_ClearsDependentSelectionsAndDfmState()
    {
        var materialId = Guid.NewGuid();
        var finishId = Guid.NewGuid();
        var toleranceId = Guid.NewGuid();
        var part = new PartViewModel
        {
            ProcessId = Guid.NewGuid(),
            ProcessCode = "FDM",
            MaterialId = materialId,
            MaterialCode = "PLA",
            FinishId = finishId,
            FinishCode = "AS_PRINTED",
            ToleranceId = toleranceId,
            ToleranceCode = "STD",
            DfmAllClearNotified = true,
            DfmAnalysisTimedOut = true,
            AnalysisErrorCode = "DFM_REPORT_UNAVAILABLE",
            DfmReport = new object(),
            LocalDfmRuntimeRunningProcessCode = "FDM",
            LocalDfmRuntimeStartedAtUtc = DateTimeOffset.UtcNow,
            LocalDfmRuntimeTerminalProcessCode = "FDM",
            LocalDfmRuntimeTerminalReason = "worker_failed",
            AvailableMaterials = [new CatalogMaterialDto(materialId, "PLA", "PLA", "Plastic", null, null, 10)],
            AvailableFinishes = [new CatalogSurfaceFinishDto(finishId, "As printed", "AS_PRINTED", null, 0m, null, 10)],
            AvailableTolerances = [new CatalogToleranceDto(toleranceId, "Standard", "STD", "ISO 2768", "m", null, 0m, 10)],
            AvailableProcessOptions =
            [
                new ProcessConfigOptionDto(Guid.NewGuid(), "infill", "Infill", "number", "20", null, "%", null, false, 10)
            ],
            ProcessOptionValues = new Dictionary<string, string?> { ["infill"] = "35" },
        };
        var cnc = new ProcessDto(Guid.NewGuid(), "CNC_MILL", "CNC Mill", null, 20);

        var changed = ProjectPartBulkEdit.ApplyProcess(part, cnc);

        Assert.True(changed);
        Assert.Equal(cnc.Id, part.ProcessId);
        Assert.Equal(cnc.Code, part.ProcessCode);
        Assert.Null(part.MaterialId);
        Assert.Null(part.MaterialCode);
        Assert.Null(part.FinishId);
        Assert.Null(part.FinishCode);
        Assert.Null(part.ToleranceId);
        Assert.Null(part.ToleranceCode);
        Assert.Empty(part.AvailableMaterials);
        Assert.Empty(part.AvailableFinishes);
        Assert.Empty(part.AvailableTolerances);
        Assert.Empty(part.AvailableProcessOptions);
        Assert.Empty(part.ProcessOptionValues);
        Assert.Null(part.DfmReport);
        Assert.False(part.DfmAllClearNotified);
        Assert.False(part.DfmAnalysisTimedOut);
        Assert.Null(part.AnalysisErrorCode);
        Assert.Null(part.LocalDfmRuntimeRunningProcessCode);
        Assert.Null(part.LocalDfmRuntimeStartedAtUtc);
        Assert.Null(part.LocalDfmRuntimeTerminalProcessCode);
        Assert.Null(part.LocalDfmRuntimeTerminalReason);
    }

    [Fact]
    public void ApplyProcess_WhenProcessIsSame_DoesNotClearExistingSelections()
    {
        var processId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var part = new PartViewModel
        {
            ProcessId = processId,
            ProcessCode = "FDM",
            MaterialId = materialId,
            MaterialCode = "PLA",
            AvailableMaterials = [new CatalogMaterialDto(materialId, "PLA", "PLA", "Plastic", null, null, 10)],
        };

        var changed = ProjectPartBulkEdit.ApplyProcess(part, new ProcessDto(processId, "FDM", "FDM", null, 10));

        Assert.False(changed);
        Assert.Equal(materialId, part.MaterialId);
        Assert.Equal("PLA", part.MaterialCode);
        Assert.Single(part.AvailableMaterials);
    }

    [Fact]
    public void ApplyPatch_WhenFieldIsNotIncluded_DoesNotOverwriteExistingValue()
    {
        var part = new PartViewModel
        {
            Quantity = 4,
            InspectionLevel = InspectionLevel.Dimensional,
        };
        var patch = new PartConfigurationBulkPatch
        {
            IncludeQuantity = false,
            Quantity = 25,
            IncludeInspection = true,
            InspectionLevel = InspectionLevel.Standard,
        };

        var result = ProjectPartBulkEdit.ApplyPatch(part, patch);

        Assert.Equal(4, part.Quantity);
        Assert.Equal(InspectionLevel.Standard, part.InspectionLevel);
        Assert.Contains("Inspection", result.AppliedFields);
        Assert.DoesNotContain("Quantity", result.AppliedFields);
        Assert.DoesNotContain("Quantity", result.SkippedFields);
    }

    [Fact]
    public void ApplyPatch_WhenTargetDoesNotContainSelectedMaterial_SkipsMaterial()
    {
        var existingMaterialId = Guid.NewGuid();
        var unavailableMaterial = new CatalogMaterialDto(Guid.NewGuid(), "Aluminum 6061", "AL6061", "Metal", null, null, 10);
        var part = new PartViewModel
        {
            MaterialId = existingMaterialId,
            MaterialCode = "PLA",
            AvailableMaterials = [new CatalogMaterialDto(existingMaterialId, "PLA", "PLA", "Plastic", null, null, 10)],
        };
        var patch = new PartConfigurationBulkPatch
        {
            IncludeMaterial = true,
            Material = unavailableMaterial,
        };

        var result = ProjectPartBulkEdit.ApplyPatch(part, patch);

        Assert.Equal(existingMaterialId, part.MaterialId);
        Assert.Equal("PLA", part.MaterialCode);
        Assert.Empty(result.AppliedFields);
        Assert.Contains("Material", result.SkippedFields);
    }

    [Fact]
    public void ApplyPatch_WhenTargetContainsSelectedTolerance_AppliesTolerance()
    {
        var tolerance = new CatalogToleranceDto(Guid.NewGuid(), "ISO 2768 Fine", "ISO2768_F", "ISO 2768", "f", null, 12m, 20);
        var part = new PartViewModel
        {
            AvailableTolerances = [tolerance],
        };
        var patch = new PartConfigurationBulkPatch
        {
            IncludeTolerance = true,
            Tolerance = tolerance,
        };

        var result = ProjectPartBulkEdit.ApplyPatch(part, patch);

        Assert.Equal(tolerance.Id, part.ToleranceId);
        Assert.Equal(tolerance.Code, part.ToleranceCode);
        Assert.Contains("Tolerance", result.AppliedFields);
        Assert.Empty(result.SkippedFields);
    }
}

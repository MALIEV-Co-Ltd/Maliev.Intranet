using System.Reflection;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;

namespace Maliev.Intranet.Tests.Client.Components;

public class PartConfigSidebarTests
{
    [Fact]
    public async Task OnParametersSetAsync_WhenPartChanges_ClearsDfmCache()
    {
        var sidebar = new PartConfigSidebar();
        SetPartParameter(sidebar, new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "first.stl",
        });

        await InvokeOnParametersSetAsync(sidebar);

        var dfmReports = GetPrivateField<Dictionary<string, DfmAnalysisResponse>>(sidebar, "_dfmReports");
        var typedReports = GetPrivateField<Dictionary<string, object>>(sidebar, "_typedReportsByProcess");

        dfmReports["FDM"] = new DfmAnalysisResponse
        {
            ProcessCode = "FDM",
            Status = "analysis_complete",
            DfmReport = new DfmReport { ReportType = "FDM" },
        };
        typedReports["FDM"] = new DfmReport { ReportType = "FDM" };

        SetPartParameter(sidebar, new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "second.stl",
        });

        await InvokeOnParametersSetAsync(sidebar);

        Assert.Empty(dfmReports);
        Assert.Empty(typedReports);
    }

    [Fact]
    public async Task OnCustomPaintColorSelected_WhenStandardColorWasSelected_SwitchesToCustomColor()
    {
        var sidebar = new PartConfigSidebar();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "painted.stl",
            ProcessOptionValues =
            {
                ["paint_color_hex"] = "#111111",
                ["paint_color_reference"] = "RAL 9005",
                ["paint_color"] = "RAL 9005",
            },
        };
        SetPartParameter(sidebar, part);

        await InvokePrivateTask(sidebar, "OnCustomPaintColorSelected", "paint_color");

        Assert.Equal("#000000", part.ProcessOptionValues["paint_color_hex"]);
        Assert.False(part.ProcessOptionValues.ContainsKey("paint_color_reference"));
        Assert.True(IsCustomPaintColorSelected(part.ProcessOptionValues["paint_color_hex"], null));
    }

    [Fact]
    public void GetVisibleFinishes_WhenAnodizeColorsShareType_ShowsOneTypeTwoAndOneTypeThree()
    {
        var finishes = new List<CatalogSurfaceFinishDto>
        {
            new(Guid.NewGuid(), "Anodized Natural (Type II)", "ANODIZE_CLEAR", 1.6m, 12m, null, 30),
            new(Guid.NewGuid(), "Anodized Black (Type II)", "ANODIZE_BLACK", 1.6m, 15m, null, 40),
            new(Guid.NewGuid(), "Hard Anodized (Type III)", "ANODIZE_HARD", 1.0m, 25m, null, 50),
        };

        var visible = GetVisibleFinishes(finishes).ToList();

        Assert.Equal(2, visible.Count);
        Assert.Contains(visible, f => GetSurfaceFinishDisplayName(f) == "Anodized Type II");
        Assert.Contains(visible, f => GetSurfaceFinishDisplayName(f) == "Anodized Type III");
    }

    [Fact]
    public void IsVisibleProcessOption_WhenThreadSpecificationCatalogOption_ReturnsFalse()
    {
        var sidebar = new PartConfigSidebar();
        var option = new ProcessConfigOptionDto(
            Guid.NewGuid(),
            "thread_spec",
            "Thread Specification",
            "text",
            null,
            null,
            null,
            "e.g. M6x1.0, 1/4-20 UNC",
            false,
            30);

        Assert.False(IsVisibleProcessOption(sidebar, option));
    }

    [Fact]
    public void IsVisibleProcessOption_WhenGrooveUndercutCatalogOption_ReturnsFalse()
    {
        var sidebar = new PartConfigSidebar();
        var option = new ProcessConfigOptionDto(
            Guid.NewGuid(),
            "groove_undercut",
            "Groove/Undercut",
            "boolean",
            null,
            null,
            null,
            "Detected from drawing or model geometry",
            false,
            40);

        Assert.False(IsVisibleProcessOption(sidebar, option));
    }

    [Fact]
    public void IsVisibleProcessOption_WhenSlaUvPostCureCatalogOption_ReturnsFalse()
    {
        var sidebar = new PartConfigSidebar();
        var option = new ProcessConfigOptionDto(
            Guid.NewGuid(),
            "uv_cure",
            "UV Post-cure",
            "toggle",
            "true",
            null,
            null,
            null,
            false,
            30);

        Assert.False(IsVisibleProcessOption(sidebar, option));
    }

    [Fact]
    public void RootClass_WhenInlineDisplayMode_IncludesInlineModifier()
    {
        var sidebar = new PartConfigSidebar();
        SetDisplayModeParameter(sidebar, PartConfigSidebarDisplayMode.Inline);

        Assert.Equal("pcs-root pcs-root--inline", GetRootClass(sidebar));
    }

    [Fact]
    public void InlineLayout_UsesColumnFlowToAvoidGridRowGaps()
    {
        var source = ReadRepoFile(
            "Maliev.Intranet.Client",
            "Components",
            "Project",
            "PartConfigSidebar.razor");

        Assert.Contains("column-width: 360px", source);
        Assert.Contains("break-inside: avoid", source);
        Assert.Contains("column-span: all", source);
        Assert.Contains("column-count: 2;", source);
        Assert.Contains("@@media (max-width: 640px)", source);
        Assert.Contains("column-count: 1;\n                    column-width: auto;\n                    padding: 8px;", source.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.DoesNotContain(
            "grid-template-columns: repeat(auto-fit, minmax(min(100%, 360px), 1fr));",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ProcessCardStyles_UseFlushFullBleedImagesAndCompactLeftAlignedCopy()
    {
        var source = ReadRepoFile(
                "Maliev.Intranet.Client",
                "Components",
                "Project",
                "PartConfigSidebar.razor")
            .ReplaceLineEndings("\n");

        Assert.Contains(".pcs-process-row {\n                display: flex;\n                flex-direction: column;\n                gap: 7px;\n                overflow-y: auto;\n                padding: 0 0 6px;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-card {\n                flex: 0 0 auto;\n                width: 100%;", source, StringComparison.Ordinal);
        Assert.Contains("box-shadow: inset 0 0 0 1px var(--maliev-border);", source, StringComparison.Ordinal);
        Assert.Contains("border: 2px solid var(--pcs-process-active);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("border-width: 2px;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-photo {\n                width: 80px;\n                height: auto;\n                min-height: 60px;", source, StringComparison.Ordinal);
        Assert.Contains("background: var(--maliev-panel-3) center / cover no-repeat;", source, StringComparison.Ordinal);
        Assert.Contains("display: block;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-copy {\n                flex: 1;\n                min-width: 0;\n                box-sizing: border-box;\n                padding: 7px 8px 8px;\n                text-align: left;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-name {\n                line-height: 1.2;\n                text-align: left;\n                font-weight: 700;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-name {\n                line-height: 1.2;\n                text-align: left;\n                font-weight: 700;\n                color: var(--maliev-text);\n                white-space: nowrap;\n                overflow: hidden;\n                text-overflow: ellipsis;", source, StringComparison.Ordinal);
        Assert.Contains(".pcs-process-description {\n                margin-top: 3px;\n                color: var(--maliev-muted);\n                font-size: var(--mud-typography-caption-size);\n                font-weight: 400;\n                line-height: 1.35;", source, StringComparison.Ordinal);
        Assert.Contains("white-space: nowrap;", source, StringComparison.Ordinal);
        Assert.Contains("text-overflow: ellipsis;", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnNotesChanged_WhenValueProvided_UpdatesPartNotes()
    {
        var sidebar = new PartConfigSidebar();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "noted.stl",
        };
        SetPartParameter(sidebar, part);

        await InvokePrivateTask(sidebar, "OnNotesChanged", "Deburr all outside edges.");

        Assert.Equal("Deburr all outside edges.", part.PartNotes);
    }

    [Fact]
    public async Task OnThreadedHolesChanged_WhenProcessIsNotSelected_DoesNotUpdatePart()
    {
        var sidebar = new PartConfigSidebar();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "unconfigured.stl",
        };
        SetPartParameter(sidebar, part);

        await InvokePrivateTask(sidebar, "OnThreadedHolesChanged", true);

        Assert.False(part.HasThreadedHoles);
    }

    [Fact]
    public async Task OnInsertsChanged_WhenProcessIsNotSelected_DoesNotUpdatePart()
    {
        var sidebar = new PartConfigSidebar();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "unconfigured.stl",
        };
        SetPartParameter(sidebar, part);

        await InvokePrivateTask(sidebar, "OnInsertsChanged", true);

        Assert.False(part.HasInserts);
    }

    [Fact]
    public async Task OnInspectionChanged_WhenProcessIsNotSelected_DoesNotUpdatePart()
    {
        var sidebar = new PartConfigSidebar();
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "unconfigured.stl",
        };
        SetPartParameter(sidebar, part);

        await InvokePrivateTask(sidebar, "OnInspectionChanged", InspectionLevel.FullCmm);

        Assert.Equal(InspectionLevel.Standard, part.InspectionLevel);
    }

    private static async Task InvokeOnParametersSetAsync(PartConfigSidebar sidebar)
    {
        await InvokePrivateTask(sidebar, "OnParametersSetAsync");
    }

    private static async Task InvokePrivateTask(PartConfigSidebar sidebar, string methodName, params object?[] parameters)
    {
        var method = typeof(PartConfigSidebar).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var task = (Task?)method.Invoke(sidebar, parameters);
        Assert.NotNull(task);
        await task;
    }

    private static bool IsCustomPaintColorSelected(string? paintHex, string? paintReference)
    {
        var method = typeof(PartConfigSidebar).GetMethod(
            "IsCustomPaintColorSelected",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        return Assert.IsType<bool>(method.Invoke(null, [paintHex, paintReference]));
    }

    private static IReadOnlyList<CatalogSurfaceFinishDto> GetVisibleFinishes(
        IReadOnlyList<CatalogSurfaceFinishDto> finishes)
    {
        var method = typeof(PartConfigSidebar).GetMethod(
            "GetVisibleFinishes",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        return Assert.IsAssignableFrom<IReadOnlyList<CatalogSurfaceFinishDto>>(method.Invoke(null, [finishes]));
    }

    private static string GetSurfaceFinishDisplayName(CatalogSurfaceFinishDto finish)
    {
        var method = typeof(PartConfigSidebar).GetMethod(
            "GetSurfaceFinishDisplayName",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        return Assert.IsType<string>(method.Invoke(null, [finish]));
    }

    private static bool IsVisibleProcessOption(PartConfigSidebar sidebar, ProcessConfigOptionDto option)
    {
        var method = typeof(PartConfigSidebar).GetMethod(
            "IsVisibleProcessOption",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        return Assert.IsType<bool>(method.Invoke(sidebar, [option]));
    }

    private static void SetPartParameter(PartConfigSidebar sidebar, PartViewModel part)
    {
        var property = typeof(PartConfigSidebar).GetProperty(nameof(PartConfigSidebar.Part));

        Assert.NotNull(property);
        property.SetValue(sidebar, part);
    }

    private static void SetDisplayModeParameter(
        PartConfigSidebar sidebar,
        PartConfigSidebarDisplayMode displayMode)
    {
        var property = typeof(PartConfigSidebar).GetProperty(nameof(PartConfigSidebar.DisplayMode));

        Assert.NotNull(property);
        property.SetValue(sidebar, displayMode);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        return Assert.IsType<T>(field.GetValue(instance));
    }

    private static string GetRootClass(PartConfigSidebar sidebar)
    {
        var property = typeof(PartConfigSidebar).GetProperty(
            "RootClass",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(property);
        return Assert.IsType<string>(property.GetValue(sidebar));
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}

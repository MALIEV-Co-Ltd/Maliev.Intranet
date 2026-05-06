using System.Reflection;
using Maliev.Intranet.Client.Components.Project;
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
}

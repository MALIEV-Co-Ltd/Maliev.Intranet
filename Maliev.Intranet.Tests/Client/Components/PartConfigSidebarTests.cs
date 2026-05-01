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

    private static void SetPartParameter(PartConfigSidebar sidebar, PartViewModel part)
    {
        var property = typeof(PartConfigSidebar).GetProperty(nameof(PartConfigSidebar.Part));

        Assert.NotNull(property);
        property.SetValue(sidebar, part);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        return Assert.IsType<T>(field.GetValue(instance));
    }
}

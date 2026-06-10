using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>
/// Verifies that browser-local DFM issues (face indices, no server overlay GLB)
/// are toggleable in the overlay panel just like server overlay issues.
/// </summary>
public sealed class DfmOverlayPanelLocalOverlayTests : BunitContext
{
    public DfmOverlayPanelLocalOverlayTests()
    {
        Services.AddMudServices(conf => conf.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static DfmIssue LocalOverhangIssue(IReadOnlyList<int>? faceIndices) => new(
        Icon: Icons.Material.Filled.Layers,
        Title: "Local support risk",
        Description: "Local analysis found downward-facing faces that may require supports.",
        Severity: DfmIssueSeverity.Warning,
        Category: "overhang",
        OverlayKey: "FDM__overhang",
        OverlayUrl: null,
        Source: "local",
        FaceIndices: faceIndices);

    private string RenderPanelMarkup(DfmIssue issue) =>
        Render<DfmOverlayPanel>(parameters => parameters
            .Add(p => p.DfmIssues, new List<DfmIssue> { issue })
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnToggleOverlay, _ => { }))
        .Markup;

    [Fact]
    public void LocalIssueWithFaceIndices_IsToggleable()
    {
        var markup = RenderPanelMarkup(LocalOverhangIssue([3, 7, 12]));

        Assert.Contains("dfm-issue-row--clickable", markup);
    }

    [Fact]
    public void LocalIssueWithoutFaceIndicesOrOverlayUrl_IsNotToggleable()
    {
        var markup = RenderPanelMarkup(LocalOverhangIssue(null));

        Assert.DoesNotContain("dfm-issue-row--clickable", markup);
    }

    [Fact]
    public void ServerIssueWithOverlayUrl_RemainsToggleable()
    {
        var serverIssue = LocalOverhangIssue(null) with
        {
            Source = "server",
            OverlayUrl = "https://storage.local/overlays/FDM__overhang.glb",
        };

        var markup = RenderPanelMarkup(serverIssue);

        Assert.Contains("dfm-issue-row--clickable", markup);
    }
}

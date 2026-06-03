using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared;

namespace Maliev.Intranet.Tests.Client.Components;

public class PartViewModelViewerSettingsTests
{
    [Fact]
    public void ToDraftPartState_RoundTripsViewerSettings_PerPart()
    {
        var firstPart = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "turning-part.stl",
            ViewerSettings = new PartViewerSettings
            {
                RenderMode = "transparent",
                CameraProjection = "orthographic",
                EdgesEnabled = true,
                GridEnabled = true,
                BoundingBoxEnabled = true,
                SectionEnabled = true,
                SectionAxis = "Y",
                SectionOffsetMm = 4.5,
                SectionInverted = true,
            },
        };
        var secondPart = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "milling-part.stl",
        };

        var restoredFirst = PartViewModel.FromDraftPartState(firstPart.ToDraftPartState());
        var restoredSecond = PartViewModel.FromDraftPartState(secondPart.ToDraftPartState());

        Assert.Equal("transparent", restoredFirst.ViewerSettings.RenderMode);
        Assert.Equal("orthographic", restoredFirst.ViewerSettings.CameraProjection);
        Assert.True(restoredFirst.ViewerSettings.EdgesEnabled);
        Assert.True(restoredFirst.ViewerSettings.GridEnabled);
        Assert.True(restoredFirst.ViewerSettings.BoundingBoxEnabled);
        Assert.True(restoredFirst.ViewerSettings.SectionEnabled);
        Assert.Equal("Y", restoredFirst.ViewerSettings.SectionAxis);
        Assert.Equal(4.5, restoredFirst.ViewerSettings.SectionOffsetMm);
        Assert.True(restoredFirst.ViewerSettings.SectionInverted);

        Assert.Equal("realistic", restoredSecond.ViewerSettings.RenderMode);
        Assert.False(restoredSecond.ViewerSettings.EdgesEnabled);
        Assert.True(restoredSecond.ViewerSettings.GridEnabled);
    }

    [Fact]
    public void ToDraftPartState_WhenGridFloorExplicitlyDisabled_PreservesDisabledSetting()
    {
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "grid-disabled-part.stl",
            ViewerSettings = new PartViewerSettings
            {
                GridEnabled = false,
            },
        };

        var restored = PartViewModel.FromDraftPartState(part.ToDraftPartState());

        Assert.False(restored.ViewerSettings.GridEnabled);
    }

    [Theory]
    [InlineData("CNC_Milling")]
    [InlineData("CNC_Turning")]
    public void ResolveDfmReport_WhenProcessCodeUsesProjectServiceEnum_UsesCncReport(string processCode)
    {
        var cncReport = new object();
        var fdmReport = new object();
        var part = new PartViewModel
        {
            ProcessCode = processCode,
            CncDfmReport = cncReport,
            FdmDfmReport = fdmReport,
        };

        part.ResolveDfmReport();

        Assert.Same(cncReport, part.DfmReport);
    }

    [Fact]
    public void FromDraftPartState_WhenProcessCodeUsesProjectServiceEnum_NormalizesForCatalogRestore()
    {
        var part = new PartViewModel
        {
            FileId = Guid.NewGuid(),
            Name = "restored-cnc.stl",
            ProcessCode = "CNC_Milling",
        };

        var restored = PartViewModel.FromDraftPartState(part.ToDraftPartState());

        Assert.Equal("CNC_MILL", restored.ProcessCode);
    }
}

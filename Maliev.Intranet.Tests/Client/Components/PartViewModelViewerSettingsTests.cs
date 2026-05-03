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

        Assert.Equal("solid", restoredSecond.ViewerSettings.RenderMode);
        Assert.False(restoredSecond.ViewerSettings.EdgesEnabled);
        Assert.False(restoredSecond.ViewerSettings.GridEnabled);
    }
}

using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class PartConfigSidebarRenderTests : BunitContext, IAsyncLifetime
{
    public PartConfigSidebarRenderTests()
    {
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton<CurrencyService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ToleranceSection_WhenIsoAndItOptionsExist_GroupsThemSeparately()
    {
        var part = new PartViewModel
        {
            FileId = Guid.Empty,
            Name = "fixture.stl",
            ProcessCode = "CNC_MILL",
            AvailableTolerances =
            [
                new CatalogToleranceDto(Guid.NewGuid(), "Fine (ISO 2768-f)", "ISO2768_F", "ISO 2768", "f", "+-0.05mm", 15m, 10),
                new CatalogToleranceDto(Guid.NewGuid(), "Medium (ISO 2768-m)", "ISO2768_M", "ISO 2768", "m", "+-0.1mm", 0m, 20),
                new CatalogToleranceDto(Guid.NewGuid(), "IT6", "IT6", "ISO 286", "IT6", null, 60m, 30),
                new CatalogToleranceDto(Guid.NewGuid(), "IT7", "IT7", "ISO 286", "IT7", null, 35m, 40),
            ],
        };

        var cut = Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, []));

        Assert.Equal(2, cut.FindAll(".pcs-tolerance-group").Count);
        Assert.Contains("General tolerances", cut.Markup);
        Assert.Contains("Fit / precision grades", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".pcs-tolerance-group--iso .pcs-tol-card"));
        Assert.NotEmpty(cut.FindAll(".pcs-tolerance-group--it .pcs-tol-card"));
    }
}

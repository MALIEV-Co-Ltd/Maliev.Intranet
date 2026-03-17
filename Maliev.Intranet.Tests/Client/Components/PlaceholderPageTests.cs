using Bunit;
using Maliev.Intranet.Client.Pages.Admin;
using Maliev.Intranet.Client.Pages.HR;
using Maliev.Intranet.Client.Pages.Finance;
using Maliev.Intranet.Client.Pages.Manufacturing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

public class PlaceholderPageTests : BunitContext, IAsyncLifetime
{
    public PlaceholderPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // Keep: Production Queue is now a real page
    [Fact] public void ProductionQueue_ShouldRenderTitle() => Assert.Contains("Production Queue", Render<ProductionQueue>().Markup, StringComparison.OrdinalIgnoreCase);
}

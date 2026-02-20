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

    [Fact] public void Audit_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Audit>().Markup, StringComparison.OrdinalIgnoreCase);
    [Fact] public void Content_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Content>().Markup, StringComparison.OrdinalIgnoreCase);
    [Fact] public void Workflows_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Workflows>().Markup, StringComparison.OrdinalIgnoreCase);
    [Fact] public void PortalConfig_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<PortalConfig>().Markup, StringComparison.OrdinalIgnoreCase);
    [Fact] public void Training_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Training>().Markup, StringComparison.OrdinalIgnoreCase);
    [Fact] public void Budget_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Budget>().Markup, StringComparison.OrdinalIgnoreCase);
    [Fact] public void ReportBuilder_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<ReportBuilder>().Markup, StringComparison.OrdinalIgnoreCase);
    [Fact] public void Inventory_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Inventory>().Markup, StringComparison.OrdinalIgnoreCase);
    [Fact] public void ProductionQueue_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<ProductionQueue>().Markup, StringComparison.OrdinalIgnoreCase);
}

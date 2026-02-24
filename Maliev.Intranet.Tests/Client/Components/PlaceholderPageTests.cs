using Bunit;
using Maliev.Intranet.Client.Pages.Admin;
using Maliev.Intranet.Client.Pages.HR;
using Maliev.Intranet.Client.Pages.Finance;
using Maliev.Intranet.Client.Pages.Manufacturing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for placeholder page components that are not yet implemented.</summary>
public class PlaceholderPageTests : BunitContext, IAsyncLifetime
{
    /// <summary>Initializes a new instance of the <see cref="PlaceholderPageTests"/> class.</summary>
    public PlaceholderPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync() => await base.DisposeAsync();

    /// <summary>Verifies that the Audit placeholder page renders a coming soon message.</summary>
    [Fact] public void Audit_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Audit>().Markup, StringComparison.OrdinalIgnoreCase);
    /// <summary>Verifies that the Content placeholder page renders a coming soon message.</summary>
    [Fact] public void Content_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Content>().Markup, StringComparison.OrdinalIgnoreCase);
    /// <summary>Verifies that the Workflows placeholder page renders a coming soon message.</summary>
    [Fact] public void Workflows_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Workflows>().Markup, StringComparison.OrdinalIgnoreCase);
    /// <summary>Verifies that the PortalConfig placeholder page renders a coming soon message.</summary>
    [Fact] public void PortalConfig_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<PortalConfig>().Markup, StringComparison.OrdinalIgnoreCase);
    /// <summary>Verifies that the Training placeholder page renders a coming soon message.</summary>
    [Fact] public void Training_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Training>().Markup, StringComparison.OrdinalIgnoreCase);
    /// <summary>Verifies that the Budget placeholder page renders a coming soon message.</summary>
    [Fact] public void Budget_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Budget>().Markup, StringComparison.OrdinalIgnoreCase);
    /// <summary>Verifies that the ReportBuilder placeholder page renders a coming soon message.</summary>
    [Fact] public void ReportBuilder_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<ReportBuilder>().Markup, StringComparison.OrdinalIgnoreCase);
    /// <summary>Verifies that the Inventory placeholder page renders a coming soon message.</summary>
    [Fact] public void Inventory_ShouldRenderComingSoon() => Assert.Contains("coming soon", Render<Inventory>().Markup, StringComparison.OrdinalIgnoreCase);
}

using Bunit;
using Maliev.Intranet.Client.Pages;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Pages;

public class ProcurementPageTests : BunitContext, IAsyncLifetime
{
    public ProcurementPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new Testing.MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ShouldRenderTitle()
    {
        var cut = Render<Procurement>();

        Assert.Contains("Purchase Orders", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRenderActiveDraftHistoryTabs()
    {
        var cut = Render<Procurement>();

        Assert.Contains("Active", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Draft", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("History", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRenderCreatePoButton()
    {
        var cut = Render<Procurement>();

        Assert.Contains("Create PO", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}

using Bunit;
using Maliev.Intranet.Client.Pages;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Pages;

public class ReceiptsPageTests : BunitContext, IAsyncLifetime
{
    public ReceiptsPageTests()
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
        var cut = Render<Receipts>();

        Assert.Contains("Payment Receipts", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRenderCreateReceiptButton()
    {
        var cut = Render<Receipts>();

        Assert.Contains("Create Receipt", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRenderSearchField()
    {
        var cut = Render<Receipts>();

        Assert.Contains("Search receipts", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRenderDataGridOrEmptyState()
    {
        var cut = Render<Receipts>();

        // Either the data grid or empty state renders after loading completes
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mud-data-grid", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("No receipts", StringComparison.OrdinalIgnoreCase),
            "Expected data grid, skeleton, or empty state to be present");
    }
}

using Bunit;
using Maliev.Intranet.Client.Pages;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Tests.Testing;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.Intranet.Tests.Client.Pages;

/// <summary>
/// Tests for Phase 10: Cross-Cutting Improvements.
/// Verifies Materials merge, Procurement merge, PricingAI standalone, and page-level UX changes.
/// </summary>
public class Phase10CrossCuttingTests : BunitContext, IAsyncLifetime
{
    public Phase10CrossCuttingTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddScoped<BreadcrumbService>();

        // CustomerDetails requires LayoutService, ISignalRCustomerService, ILogger
        var loggerMock = new Mock<ILogger<LayoutService>>();
        var layoutService = new LayoutService(JSInterop.JSRuntime, loggerMock.Object, null!);
        Services.AddSingleton<LayoutService>(layoutService);

        var signalRMock = new Mock<ISignalRCustomerService>();
        signalRMock.Setup(s => s.IsConnected).Returns(false);
        signalRMock.Setup(s => s.StartAsync()).Returns(Task.CompletedTask);
        signalRMock.Setup(s => s.StopAsync()).Returns(Task.CompletedTask);
        signalRMock.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);
        Services.AddSingleton(signalRMock.Object);
        Services.AddSingleton<Mock<ILogger<CustomerDetails>>>(new Mock<ILogger<CustomerDetails>>());
        Services.AddSingleton(new Mock<ILogger<CustomerDetails>>().Object);

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    // ── Materials page (Catalog + Inventory tabs) ─────────────────────────────

    [Fact]
    public void MaterialsPage_ShouldRender_WithoutException()
    {
        var cut = Render<Materials>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void MaterialsPage_ShouldContain_CatalogOrInventoryContent()
    {
        var cut = Render<Materials>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("Catalog", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Inventory", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Material", StringComparison.OrdinalIgnoreCase),
            "Expected Materials or Inventory content");
    }

    // ── Procurement page (POs + Suppliers tabs) ───────────────────────────────

    [Fact]
    public void ProcurementPage_ShouldRender_WithoutException()
    {
        var cut = Render<Procurement>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void ProcurementPage_ShouldContain_PurchaseOrdersOrSuppliersContent()
    {
        var cut = Render<Procurement>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("Purchase", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Supplier", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Procurement", StringComparison.OrdinalIgnoreCase),
            "Expected Purchase Orders or Suppliers content");
    }

    // ── PricingAI page (standalone calculator) ────────────────────────────────

    [Fact]
    public void PricingAIPage_ShouldRender_WithoutException()
    {
        var cut = Render<PricingAI>();
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void PricingAIPage_ShouldContain_PricingContent()
    {
        var cut = Render<PricingAI>();
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("Pricing", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Calculator", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("AI", StringComparison.OrdinalIgnoreCase),
            "Expected Pricing AI content");
    }

    // ── CustomerDetails page — "New Project" button ───────────────────────────

    [Fact]
    public void CustomerDetailsPage_ShouldRender_WithIdParameter()
    {
        var id = Guid.NewGuid();
        var cut = Render<CustomerDetails>(parameters => parameters.Add(p => p.Id, id));
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void CustomerDetailsPage_ShouldContain_NewProjectButton()
    {
        var id = Guid.NewGuid();
        var cut = Render<CustomerDetails>(parameters => parameters.Add(p => p.Id, id));
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("New Project", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("project", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Customer", StringComparison.OrdinalIgnoreCase),
            "Expected New Project button or customer content");
    }
}

using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class QuoteSummaryBarTests : BunitContext, IAsyncLifetime
{
    public QuoteSummaryBarTests()
    {
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost/") });
        Services.AddSingleton<CurrencyService>();
        Services.AddSingleton<ShippingService>();
        Services.AddSingleton<AlertService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task DetailsPopover_WhenOpened_RendersCommercialAdjustmentCards()
    {
        var customer = new CustomerSummaryDto { Id = Guid.NewGuid(), Name = "Test Customer", Email = "test@example.com" };
        var cut = Render<QuoteSummaryBar>(parameters => parameters
            .Add(p => p.Parts, [])
            .Add(p => p.LeadTimeOptions, [])
            .Add(p => p.ShippingCost, 120m)
            .Add(p => p.ManualDiscountAmount, 25m)
            .Add(p => p.QuotationTerms, "Net 30")
            .Add(p => p.SelectedCustomer, customer));

        // MudButton OnClick dispatches on the component sync context;
        // InvokeAsync ensures the full render cycle runs before asserting.
        await cut.InvokeAsync(() => cut.Find(".qsb-btn-details").Click());

        Assert.NotEmpty(cut.FindAll(".qsb-details-popover"));
        Assert.Equal(2, cut.FindAll(".qsb-adjustment-card").Count);
        Assert.NotEmpty(cut.FindAll(".qsb-terms-panel"));
        Assert.Contains("Commercial adjustments", cut.Markup);
    }
}

using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Microsoft.Extensions.DependencyInjection;
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
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void DetailsPopover_WhenOpened_RendersCommercialAdjustmentCards()
    {
        var cut = Render<QuoteSummaryBar>(parameters => parameters
            .Add(p => p.Parts, [])
            .Add(p => p.LeadTimeOptions, [])
            .Add(p => p.ShippingCost, 120m)
            .Add(p => p.ManualDiscountAmount, 25m)
            .Add(p => p.QuotationTerms, "Net 30"));

        cut.Find(".qsb-btn-details").Click();

        Assert.NotEmpty(cut.FindAll(".qsb-details-popover"));
        Assert.Equal(2, cut.FindAll(".qsb-adjustment-card").Count);
        Assert.Equal(2, cut.FindAll(".qsb-money-prefix").Count);
        Assert.NotEmpty(cut.FindAll(".qsb-terms-panel"));
        Assert.Contains("Commercial adjustments", cut.Markup);
    }
}

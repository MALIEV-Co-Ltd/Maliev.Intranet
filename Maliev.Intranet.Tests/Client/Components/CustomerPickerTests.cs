using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public sealed class CustomerPickerTests : BunitContext, IAsyncLifetime
{
    private readonly List<CustomerSummaryDto> _customers =
    [
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Sarah Chen",
            CompanyName = "Axion Robotics",
            Tier = "Active"
        },
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Marcus Webb",
            CompanyName = "Axion Robotics",
            Tier = "VIP"
        }
    ];

    public CustomerPickerTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task OpenPicker_LoadsRecentCustomersAndShowsCompanyAndTier()
    {
        var queries = new List<string>();
        var cut = Render<CustomerPicker>(parameters => parameters
            .Add(p => p.SearchCustomers, (query, _) =>
            {
                queries.Add(query);
                return Task.FromResult<IEnumerable<CustomerSummaryDto>>(_customers);
            }));

        await cut.Find(".customer-picker-trigger").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal([""], queries);
            Assert.Contains("Sarah Chen", cut.Markup);
            Assert.Contains("Axion Robotics", cut.Markup);
            Assert.Contains("Active", cut.Markup);
            Assert.Contains("VIP", cut.Markup);
        });
    }

    [Fact]
    public async Task SearchInput_ReloadsCustomersWithTypedQuery()
    {
        var queries = new List<string>();
        var cut = Render<CustomerPicker>(parameters => parameters
            .Add(p => p.SearchCustomers, (query, _) =>
            {
                queries.Add(query);
                return Task.FromResult<IEnumerable<CustomerSummaryDto>>(_customers);
            }));

        await cut.Find(".customer-picker-trigger").ClickAsync(new MouseEventArgs());
        cut.Find(".customer-picker-search-input").Input("mar");

        cut.WaitForAssertion(() => Assert.Equal(["", "mar"], queries));
    }

    [Fact]
    public async Task SelectingCustomer_RaisesSelectionAndClosesPicker()
    {
        CustomerSummaryDto? selected = null;
        var cut = Render<CustomerPicker>(parameters => parameters
            .Add(p => p.SearchCustomers, (_, _) => Task.FromResult<IEnumerable<CustomerSummaryDto>>(_customers))
            .Add(p => p.OnCustomerSelected, customer => selected = customer));

        await cut.Find(".customer-picker-trigger").ClickAsync(new MouseEventArgs());
        await cut.FindAll(".customer-picker-option").ElementAt(1).ClickAsync(new MouseEventArgs());

        Assert.Equal(_customers[1].Id, selected?.Id);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".customer-picker-popover")));
    }
}

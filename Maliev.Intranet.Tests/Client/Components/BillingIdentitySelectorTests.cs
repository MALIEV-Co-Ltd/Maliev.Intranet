using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Components;

public class BillingIdentitySelectorTests : BunitContext, IAsyncLifetime
{
    public BillingIdentitySelectorTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public void ShouldRenderLoading_WhenCustomerIsNull()
    {
        var cut = Render<BillingIdentitySelector>(parameters => parameters
            .Add(p => p.Customer, null)
        );

        Assert.Contains("Loading customer information...", cut.Markup);
    }

    [Fact]
    public void ShouldRenderPersonal_WhenOnlyPersonalExists()
    {
        var customer = new CustomerIdentityDto
        {
            FirstName = "John",
            LastName = "Doe",
            ThaiNationalIdMasked = "***-***-123"
        };

        var cut = Render<BillingIdentitySelector>(parameters => parameters
            .Add(p => p.Customer, customer)
        );

        Assert.Contains("Billing Identity: Personal", cut.Markup);
        Assert.Contains("John Doe", cut.Markup);
    }

    [Fact]
    public void ShouldRenderRadios_WhenBothExist()
    {
        var customer = new CustomerIdentityDto
        {
            FirstName = "John",
            LastName = "Doe",
            ThaiNationalIdMasked = "***-***-123",
            CompanyId = Guid.NewGuid(),
            CompanyName = "ACME Corp",
            CompanyTaxId = "123456789"
        };

        var cut = Render<BillingIdentitySelector>(parameters => parameters
            .Add(p => p.Customer, customer)
        );

        Assert.Contains("Personal", cut.Markup);
        Assert.Contains("Corporate", cut.Markup);
        Assert.Contains("ACME Corp", cut.Markup);
    }
}

using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the BillingIdentitySelector component.</summary>
public class BillingIdentitySelectorTests : BunitContext, IAsyncLifetime
{
    /// <summary>Initializes a new instance of the <see cref="BillingIdentitySelectorTests"/> class.</summary>
    public BillingIdentitySelectorTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    /// <summary>Verifies that a loading message is rendered when the customer is null.</summary>
    public void ShouldRenderLoading_WhenCustomerIsNull()
    {
        var cut = Render<BillingIdentitySelector>(parameters => parameters
            .Add(p => p.Customer, null)
        );

        Assert.Contains("Loading customer information...", cut.Markup);
    }

    [Fact]
    /// <summary>Verifies that the personal billing identity is rendered when only a personal identity exists.</summary>
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
    /// <summary>Verifies that radio buttons for both personal and corporate identities are rendered when both exist.</summary>
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

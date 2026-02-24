using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the AddressCard component.</summary>
public class AddressCardTests : BunitContext, IAsyncLifetime
{
    /// <summary>Initializes a new instance of the <see cref="AddressCardTests"/> class.</summary>
    public AddressCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    /// <summary>Verifies that the address details are rendered correctly.</summary>
    public void ShouldRenderAddressDetails()
    {
        var address = new CreateAddressRequest
        {
            Type = "Billing",
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            PostalCode = "10110",
            IsDefault = true
        };

        var cut = Render<AddressCard>(parameters => parameters
            .Add(p => p.Address, address)
            .Add(p => p.Index, 0)
        );

        Assert.Contains("123 Main St", cut.Markup);
        Assert.Contains("Bangkok", cut.Markup);
    }
}

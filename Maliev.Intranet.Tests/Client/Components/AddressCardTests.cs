using Bunit;
using Maliev.Intranet.Client.Components;
using Maliev.Intranet.Shared;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

public class AddressCardTests : BunitContext, IAsyncLifetime
{
    public AddressCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
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

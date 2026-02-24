using Bunit;
using Maliev.Intranet.Client.Components;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Components;

/// <summary>Tests for the PhoneContactFields component.</summary>
public class PhoneContactFieldsTests : BunitContext, IAsyncLifetime
{
    /// <summary>Initializes a new instance of the <see cref="PhoneContactFieldsTests"/> class.</summary>
    public PhoneContactFieldsTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Initializes the test asynchronously.</summary>
    public Task InitializeAsync() => Task.CompletedTask;
    /// <summary>Disposes resources used by the test asynchronously.</summary>
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    /// <summary>Verifies that the basic phone contact fields are rendered.</summary>
    public void ShouldRenderBasicFields()
    {
        var cut = Render<PhoneContactFields>(parameters => parameters
            .Add(p => p.Mobile, "+66812345678")
            .Add(p => p.ShowCorporateFields, false)
        );

        Assert.Contains("Personal Contact", cut.Markup);
        Assert.Contains("+66812345678", cut.Markup);
    }
}

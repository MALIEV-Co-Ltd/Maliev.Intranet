using Bunit;
using Maliev.Intranet.Client.Components;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Components;

public class PhoneContactFieldsTests : BunitContext, IAsyncLifetime
{
    public PhoneContactFieldsTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
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

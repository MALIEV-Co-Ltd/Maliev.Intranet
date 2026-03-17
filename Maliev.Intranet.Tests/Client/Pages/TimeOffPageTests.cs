using Bunit;
using Maliev.Intranet.Client.Pages;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Maliev.Intranet.Tests.Client.Pages;

public class TimeOffPageTests : BunitContext, IAsyncLifetime
{
    private NavigationManager? _nav;

    public TimeOffPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new Testing.MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Render<MudPopoverProvider>();
        _nav = Services.GetRequiredService<NavigationManager>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ShouldRedirectToLeaveTab()
    {
        var cut = Render<TimeOff>();

        Assert.Contains("/hr/leave?tab=timeoff", _nav?.Uri, StringComparison.OrdinalIgnoreCase);
    }
}

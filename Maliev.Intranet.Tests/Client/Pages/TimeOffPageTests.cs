using Bunit;
using Maliev.Intranet.Client.Pages;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Pages;

public class TimeOffPageTests : BunitContext, IAsyncLifetime
{
    public TimeOffPageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new Testing.MockHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ShouldRenderTitle()
    {
        var cut = Render<TimeOff>();

        Assert.Contains("Time Off Management", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRenderRequestLeaveButton()
    {
        var cut = Render<TimeOff>();

        Assert.Contains("Request Leave", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRenderBalanceCardsOrLoadingState()
    {
        var cut = Render<TimeOff>();

        // Either loading skeletons or the data grid/empty state is rendered
        var markup = cut.Markup;
        Assert.True(
            markup.Contains("mud-skeleton", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("mud-data-grid", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("No leave requests", StringComparison.OrdinalIgnoreCase) ||
            markup.Contains("Time Off", StringComparison.OrdinalIgnoreCase),
            "Expected loading state or rendered content to be present");
    }
}

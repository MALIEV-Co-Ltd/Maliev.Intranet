using Bunit;
using Maliev.Intranet.Client.Components.Shared;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Client.Components;

public class SharedDesignComponentTests : BunitContext, IAsyncLifetime
{
    public SharedDesignComponentTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void StatusBadge_Normalizes_StatusClassAndDisplayText()
    {
        var cut = Render<StatusBadge>(parameters => parameters.Add(p => p.Status, "in progress"));

        Assert.Contains("status-in-progress", cut.Markup);
        Assert.Contains("In Progress", cut.Markup);
    }

    [Fact]
    public void StatusBadge_DefinesSystemHealthStateColors()
    {
        var styles = ReadRepoFile("Maliev.Intranet.Client", "Components", "Shared", "StatusBadge.razor.css");

        Assert.Contains(".status-healthy", styles, StringComparison.Ordinal);
        Assert.Contains("var(--maliev-ok-bg)", styles, StringComparison.Ordinal);
        Assert.Contains(".status-unreachable", styles, StringComparison.Ordinal);
        Assert.Contains(".status-unhealthy", styles, StringComparison.Ordinal);
        Assert.Contains("var(--maliev-danger-bg)", styles, StringComparison.Ordinal);
        Assert.Contains(".status-degraded", styles, StringComparison.Ordinal);
        Assert.Contains("var(--maliev-warn-bg)", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Avatar_RendersInitials_FromEmployeeName()
    {
        var cut = Render<Avatar>(parameters => parameters.Add(p => p.Name, "Nattapol Thanakit"));

        Assert.Contains("NT", cut.Markup);
    }

    [Fact]
    public void ModuleHeader_RendersTitleSubtitleAndActions()
    {
        var cut = Render<ModuleHeader>(parameters => parameters
            .Add(p => p.Title, "Customers")
            .Add(p => p.Subtitle, "Account operations")
            .AddChildContent("<button>Create</button>"));

        Assert.Contains("Customers", cut.Markup);
        Assert.Contains("Account operations", cut.Markup);
        Assert.Contains("Create", cut.Markup);
    }

    [Fact]
    public void SearchBox_RaisesValueChanged_OnInput()
    {
        string? value = null;
        var cut = Render<SearchBox>(parameters => parameters
            .Add(p => p.Placeholder, "Search customers")
            .Add(p => p.ValueChanged, next => value = next));

        cut.Find("input").Input("acme");

        Assert.Equal("acme", value);
        Assert.Contains("Search customers", cut.Markup);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}

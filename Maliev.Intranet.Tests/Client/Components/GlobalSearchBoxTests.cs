using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Client.Components.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public class GlobalSearchBoxTests
{
    [Fact]
    public async Task GlobalSearchBox_DoesNotSearchBelowTwoCharacters()
    {
        var calls = 0;
        using var context = CreateContext(_ =>
        {
            calls++;
            return JsonContent.Create(new GlobalSearchResponseDto("a", 0, []));
        });

        var cut = context.Render<GlobalSearchBox>();
        cut.Find("input").Input("a");

        await Task.Delay(350);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void GlobalSearchBox_RendersResultsAndNavigatesOnClick()
    {
        var customerId = Guid.NewGuid();
        using var context = CreateContext(_ => JsonContent.Create(new GlobalSearchResponseDto(
            "acme",
            1,
            [
                new GlobalSearchResultDto(
                    "Acme Corp",
                    "Customer",
                    "Sales & CRM",
                    "customer",
                    "Active",
                    $"/customers/{customerId}",
                    1.0d)
            ])));

        var cut = context.Render<GlobalSearchBox>();
        cut.Find("input").Input("acme");

        cut.WaitForAssertion(() => Assert.Contains("Acme Corp", cut.Markup));
        cut.Find(".global-search-result").Click();

        var navigation = context.Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith($"/customers/{customerId}", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalSearchBox_EnterNavigatesFirstResult()
    {
        var projectId = Guid.NewGuid();
        using var context = CreateContext(_ => JsonContent.Create(new GlobalSearchResponseDto(
            "fixture",
            1,
            [
                new GlobalSearchResultDto(
                    "Fixture",
                    "Project",
                    "Sales & CRM",
                    "project",
                    "Draft",
                    $"/sales/projects/{projectId}",
                    1.0d)
            ])));

        var cut = context.Render<GlobalSearchBox>();
        var input = cut.Find("input");
        input.Input("fixture");

        cut.WaitForAssertion(() => Assert.Contains("Fixture", cut.Markup));
        input.KeyDown("Enter");

        var navigation = context.Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith($"/sales/projects/{projectId}", navigation.Uri, StringComparison.Ordinal);
    }

    private static BunitContext CreateContext(Func<HttpRequestMessage, HttpContent> contentFactory)
    {
        var context = new BunitContext();
        context.Services.AddMudServices();
        context.Services.AddSingleton(new HttpClient(new MockHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = contentFactory(request)
            })))
        {
            BaseAddress = new Uri("http://test/")
        });

        return context;
    }
}

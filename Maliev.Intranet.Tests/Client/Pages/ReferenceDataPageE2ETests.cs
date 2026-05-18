using System.Net;
using System.Net.Http.Json;
using Bunit;
using Maliev.Intranet.Client.Pages.Admin;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Pages;

public sealed class ReferenceDataPageE2ETests : BunitContext, IAsyncLifetime
{
    private readonly List<string> _requestedPaths = [];
    private string? _postedLocationJson;

    public ReferenceDataPageE2ETests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new MockHttpMessageHandler(HandleRequestAsync);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);
        Services.AddSingleton<IReferenceDataService>(new ClientReferenceDataService(client, NullLogger<ClientReferenceDataService>.Instance));

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ReferenceData_RegistrySection_RendersPaginatedCrudWorkbench()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://test/admin/reference-data?section=registry");

        var cut = Render<ReferenceData>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Thai address registry", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Khlong Khoi", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("1-1 of 1 locations", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("New location", cut.Markup, StringComparison.Ordinal);
        });

        Assert.Contains(_requestedPaths, path => path.StartsWith("/api/v1/ReferenceData/locations?pageNumber=1&pageSize=25", StringComparison.Ordinal));
    }

    [Fact]
    public void ReferenceData_CreateLocation_PostsThroughBffAndReloadsPage()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://test/admin/reference-data?section=registry");
        var cut = Render<ReferenceData>();

        cut.WaitForAssertion(() => Assert.Contains("New location", cut.Markup, StringComparison.Ordinal));
        cut.FindAll("button").Single(button => button.TextContent.Contains("New location", StringComparison.Ordinal)).Click();

        Assert.Equal(7, cut.FindAll(".reference-editor input").Count);
        InputEditor(0, "10210");
        InputEditor(1, "กรุงเทพมหานคร");
        InputEditor(2, "หลักสี่");
        InputEditor(3, "ทุ่งสองห้อง");
        InputEditor(4, "Bangkok");
        InputEditor(5, "Lak Si");
        InputEditor(6, "Thung Song Hong");

        cut.FindAll("button").Single(button => button.TextContent.Contains("Save location", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(_postedLocationJson);
            Assert.Contains("\"postalCode\":\"10210\"", _postedLocationJson, StringComparison.Ordinal);
            Assert.Contains("\"districtEn\":\"Lak Si\"", _postedLocationJson, StringComparison.Ordinal);
            Assert.Contains(_requestedPaths, path => path.Equals("/api/v1/ReferenceData/locations", StringComparison.Ordinal));
        });

        void InputEditor(int index, string value)
        {
            cut.FindAll(".reference-editor input").ToList()[index].Input(value);
        }
    }

    private async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.PathAndQuery;
        _requestedPaths.Add(path);

        if (request.Method == HttpMethod.Get && path == "/api/v1/ReferenceData/countries")
        {
            return JsonResponse(new List<CountryDto>
            {
                new()
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Code = "TH",
                    Iso3 = "THA",
                    Name = "Thailand",
                    IsActive = true
                }
            });
        }

        if (request.Method == HttpMethod.Get && path.StartsWith("/api/v1/ReferenceData/countries/page", StringComparison.Ordinal))
        {
            return JsonResponse(new ReferenceDataPage<CountryDto>
            {
                Items =
                [
                    new()
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                        Code = "TH",
                        Iso3 = "THA",
                        Name = "Thailand",
                        Region = "Asia",
                        IsActive = true
                    }
                ],
                PageNumber = 1,
                PageSize = 25,
                TotalCount = 1,
                TotalPages = 1
            });
        }

        if (request.Method == HttpMethod.Get && path == "/api/v1/ReferenceData/currencies")
        {
            return JsonResponse(new List<CurrencyDto>
            {
                new()
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    Code = "THB",
                    Symbol = "฿",
                    Name = "Thai baht",
                    DecimalPlaces = 2,
                    IsActive = true,
                    IsPrimary = true
                }
            });
        }

        if (request.Method == HttpMethod.Get && path.StartsWith("/api/v1/ReferenceData/currencies/page", StringComparison.Ordinal))
        {
            return JsonResponse(new ReferenceDataPage<CurrencyDto>
            {
                Items =
                [
                    new()
                    {
                        Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                        Code = "THB",
                        Symbol = "฿",
                        Name = "Thai baht",
                        DecimalPlaces = 2,
                        IsActive = true,
                        IsPrimary = true
                    }
                ],
                PageNumber = 1,
                PageSize = 25,
                TotalCount = 1,
                TotalPages = 1
            });
        }

        if (request.Method == HttpMethod.Get && path == "/api/v1/ReferenceData/currencies/primary")
        {
            return JsonResponse(new CurrencyDto
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Code = "THB",
                Symbol = "฿",
                Name = "Thai baht",
                DecimalPlaces = 2,
                IsActive = true,
                IsPrimary = true
            });
        }

        if (request.Method == HttpMethod.Get && path.StartsWith("/api/v1/ReferenceData/locations", StringComparison.Ordinal))
        {
            return JsonResponse(new ReferenceDataPage<RegistryThaiLocation>
            {
                Items =
                [
                    new()
                    {
                        Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                        PostalCode = "11120",
                        SubDistrictTh = "คลองข่อย",
                        DistrictTh = "ปากเกร็ด",
                        ProvinceTh = "นนทบุรี",
                        SubDistrictEn = "Khlong Khoi",
                        DistrictEn = "Pak Kret",
                        ProvinceEn = "Nonthaburi"
                    }
                ],
                PageNumber = 1,
                PageSize = 25,
                TotalCount = 1,
                TotalPages = 1
            });
        }

        if (request.Method == HttpMethod.Post && path == "/api/v1/ReferenceData/locations")
        {
            _postedLocationJson = await request.Content!.ReadAsStringAsync(cancellationToken);
            return JsonResponse(new RegistryThaiLocation
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                PostalCode = "10210",
                SubDistrictTh = "ทุ่งสองห้อง",
                DistrictTh = "หลักสี่",
                ProvinceTh = "กรุงเทพมหานคร",
                SubDistrictEn = "Thung Song Hong",
                DistrictEn = "Lak Si",
                ProvinceEn = "Bangkok"
            }, HttpStatusCode.Created);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(path)
        };
    }

    private static HttpResponseMessage JsonResponse<T>(T body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(body)
        };
    }

}

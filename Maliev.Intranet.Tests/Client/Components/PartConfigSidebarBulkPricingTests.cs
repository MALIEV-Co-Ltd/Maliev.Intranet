using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bunit;
using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Client.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

namespace Maliev.Intranet.Tests.Client.Components;

public class PartConfigSidebarBulkPricingTests : BunitContext, IAsyncLifetime
{
    private readonly MockHttpMessageHandler _httpHandler = new();
    private readonly List<HttpRequestMessage> _sentRequests = [];
    private readonly Guid _partAFileId = Guid.NewGuid();
    private readonly Guid _partBFileId = Guid.NewGuid();
    private readonly List<ProcessDto> _processes = [];

    public PartConfigSidebarBulkPricingTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _httpHandler.HandlerFunc = HandleRequest;
        var client = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://test/") };
        Services.AddSingleton(client);

        Services.AddSingleton(new FileTypesSettings
        {
            ThreeDExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".stl" },
            DocumentExtensions = [],
            ImageExtensions = [],
            OfficeExtensions = [],
            ArchiveExtensions = [],
            DrawingExtensions = [],
            SupplementaryExtensions = [],
        });
        Services.AddSingleton(new UploadSettings());

        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new async Task DisposeAsync() => await base.DisposeAsync();

    private Task<HttpResponseMessage> HandleRequest(HttpRequestMessage request, CancellationToken ct)
    {
        lock (_sentRequests) { _sentRequests.Add(request); }

        var path = request.RequestUri?.AbsolutePath ?? "";

        if (path.Contains("pricing/bulk") && request.Method == HttpMethod.Post)
        {
            var content = request.Content!.ReadAsStringAsync(ct).Result;
            var body = JsonSerializer.Deserialize<BulkPricingRequestDto>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (body == null)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));

            var tiers = body.Quantities.Select(q =>
            {
                var discountFactor = q >= 10 ? 0.90m : q >= 5 ? 0.95m : 1.0m;
                var unitPrice = Math.Round(body.BaseUnitPrice * discountFactor, 2);
                return new BulkPriceTierDto(q, unitPrice, Math.Round(unitPrice * q, 2), Math.Round((1 - discountFactor) * 100, 2));
            }).ToList();

            var json = JsonSerializer.Serialize(tiers);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, new MediaTypeHeaderValue("application/json"))
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", new MediaTypeHeaderValue("application/json"))
        });
    }

    private static PartViewModel CreatePart(Guid fileId, decimal unitPrice, int quantity = 1)
    {
        return new PartViewModel
        {
            FileId = fileId,
            Name = $"Part {fileId:N}",
            Quantity = quantity,
            EstimatedUnitPrice = unitPrice,
            EstimatedTotalAmount = unitPrice * quantity,
        };
    }

    private RenderedComponent<PartConfigSidebar> RenderSidebar(PartViewModel part)
    {
        return Render<PartConfigSidebar>(parameters => parameters
            .Add(p => p.Part, part)
            .Add(p => p.Processes, _processes)
            .Add(p => p.TempProjectId, Guid.NewGuid())
            .Add(p => p.CurrencySymbol, "฿"));
    }

    private int BulkRequestCount
    {
        get
        {
            lock (_sentRequests)
            {
                return _sentRequests.Count(r =>
                    r.RequestUri?.AbsolutePath.Contains("pricing/bulk") == true);
            }
        }
    }

    private List<HttpRequestMessage> BulkRequests
    {
        get
        {
            lock (_sentRequests)
            {
                return _sentRequests
                    .Where(r => r.RequestUri?.AbsolutePath.Contains("pricing/bulk") == true)
                    .ToList();
            }
        }
    }

    private async Task<BulkPricingRequestDto?> ReadBulkRequestBody(HttpRequestMessage req)
    {
        var json = await req.Content!.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BulkPricingRequestDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    [Fact]
    public async Task BulkPricing_AppliesDiscountToPartPrice()
    {
        var part = CreatePart(_partAFileId, 1000m, quantity: 10);
        var cut = RenderSidebar(part);

        cut.WaitForState(() => part.EstimatedUnitPrice == 900m, timeout: TimeSpan.FromSeconds(5));
        Assert.Equal(900m, part.EstimatedUnitPrice);
        Assert.Equal(9000m, part.EstimatedTotalAmount);
    }

    [Fact]
    public async Task BulkPricing_SendsOriginalBasePrice_NotDiscountedPrice()
    {
        var part = CreatePart(_partAFileId, 1000m, quantity: 10);
        var cut = RenderSidebar(part);

        cut.WaitForState(() => part.EstimatedUnitPrice == 900m, timeout: TimeSpan.FromSeconds(5));

        var bulkRequests = BulkRequests;
        Assert.NotEmpty(bulkRequests);

        foreach (var req in bulkRequests)
        {
            var body = await req.Content!.ReadFromJsonAsync<BulkPricingRequestDto>();
            Assert.NotNull(body);
            Assert.Equal(1000m, body.BaseUnitPrice);
        }
    }

    [Fact]
    public async Task BulkPricing_WhenPartHasNoFileId_DoesNotFetchBulkTiers()
    {
        var part = CreatePart(Guid.Empty, 1000m, quantity: 10);
        var cut = RenderSidebar(part);

        await Task.Delay(1000);

        Assert.Equal(0, BulkRequestCount);
    }

    [Fact]
    public async Task BulkPricing_WhenPartHasNoPrice_DoesNotFetchBulkTiers()
    {
        var part = CreatePart(_partAFileId, unitPrice: 0, quantity: 10);
        part.EstimatedUnitPrice = null;
        var cut = RenderSidebar(part);

        await Task.Delay(1000);

        Assert.Equal(0, BulkRequestCount);
    }

    [Fact]
    public async Task BulkPricing_DifferentPartsGetDifferentBasePrices()
    {
        var partA = CreatePart(_partAFileId, 1000m, quantity: 10);
        var cutA = RenderSidebar(partA);

        cutA.WaitForState(() => partA.EstimatedUnitPrice == 900m, timeout: TimeSpan.FromSeconds(5));

        var partB = CreatePart(_partBFileId, 1200m, quantity: 10);
        var cutB = RenderSidebar(partB);

        cutB.WaitForState(() => partB.EstimatedUnitPrice == 1080m, timeout: TimeSpan.FromSeconds(5));

        var allBases = new List<decimal>();
        foreach (var req in BulkRequests)
        {
            var body = await ReadBulkRequestBody(req);
            if (body != null)
                allBases.Add(body.BaseUnitPrice);
        }

        Assert.Contains(1000m, allBases);
        Assert.Contains(1200m, allBases);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class MaterialServiceClientTests
{
    [Fact]
    public async Task CreateMaterialAsync_MapsIntranetFieldsToMaterialServiceWireShape()
    {
        CapturedMaterialCreate? captured = null;
        var created = CreateServiceMaterial(
            name: "E2E PEEK",
            code: "E2E-PEEK",
            pricePerUnit: 125.75m,
            stockLevel: 42);
        var client = CreateClient(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/material/v1/materials", request.RequestUri?.AbsolutePath);
            var payload = await request.Content!.ReadAsStringAsync(ct);
            captured = JsonSerializer.Deserialize<CapturedMaterialCreate>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return JsonResponse(created);
        });

        var result = await client.CreateMaterialAsync(new CreateMaterialRequest
        {
            Name = " E2E PEEK ",
            SKU = " E2E-PEEK ",
            Description = " High performance production material ",
            UnitPrice = 125.75m,
            QuantityOnHand = 42,
            Unit = "pcs"
        });

        Assert.NotNull(result);
        Assert.NotNull(captured);
        Assert.Equal("E2E PEEK", captured.Name);
        Assert.Equal("E2E-PEEK", captured.Code);
        Assert.Equal("High performance production material", captured.Description);
        Assert.Equal(125.75m, captured.PricePerUnit);
        Assert.Equal(42, captured.StockLevel);
        Assert.Empty(captured.ManufacturingProcessIds);
        Assert.Empty(captured.ColorIds);
        Assert.Empty(captured.PostProcessingMethodIds);
        Assert.Empty(captured.MechanicalProperties);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("E2E-PEEK", result.SKU);
    }

    [Fact]
    public async Task GetMaterialByIdAsync_WhenMaterialHasProcessesColorsAndProperties_MapsDetailFields()
    {
        var material = CreateServiceMaterial();
        var client = CreateClient((_, _) => Task.FromResult(JsonResponse(material)));

        var result = await client.GetMaterialByIdAsync(material.Id);

        Assert.NotNull(result);
        Assert.Equal(["CNC Milling", "CNC Turning"], result.ManufacturingProcesses);
        Assert.Equal("Natural, Black", result.Color);
        Assert.Equal(2, result.Colors.Count);
        Assert.Equal("#d9c7a4", result.Colors[0].HexCode);
        var property = Assert.Single(result.Properties, item => item.Key == "Tensile Strength");
        Assert.Equal("90.00", property.Value);
        Assert.Equal("MPa", property.Unit);
    }

    [Fact]
    public async Task UpdateMaterialAsync_PreservesDownstreamRelationsAndMapsEditableFields()
    {
        var material = CreateServiceMaterial();
        CapturedMaterialUpdate? captured = null;
        var client = CreateClient(async (request, ct) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse(material);
            }

            var payload = await request.Content!.ReadAsStringAsync(ct);
            captured = JsonSerializer.Deserialize<CapturedMaterialUpdate>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return JsonResponse(CreateServiceMaterial(name: "PEEK GF", code: "PEEK-GF", pricePerUnit: 12.5m, stockLevel: 8));
        });

        var result = await client.UpdateMaterialAsync(material.Id, new UpdateMaterialRequest
        {
            Name = "PEEK GF",
            SKU = "PEEK-GF",
            Description = "Updated PEEK",
            UnitPrice = 12.5m,
            QuantityOnHand = 8
        });

        Assert.NotNull(result);
        Assert.NotNull(captured);
        Assert.Equal("PEEK GF", captured.Name);
        Assert.Equal("PEEK-GF", captured.Code);
        Assert.Equal(12.5m, captured.PricePerUnit);
        Assert.Equal(8, captured.StockLevel);
        Assert.Equal(material.ManufacturingProcesses.Select(process => process.Id), captured.ManufacturingProcessIds);
        Assert.Equal(material.AvailableColors.Select(color => color.Id), captured.ColorIds);
        Assert.Equal(material.PostProcessingMethods.Select(method => method.Id), captured.PostProcessingMethodIds);
        Assert.Equal(material.MechanicalProperties.Select(property => property.MechanicalPropertyId), captured.MechanicalProperties.Select(property => property.MechanicalPropertyId));
    }

    private static MaterialServiceClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        return new MaterialServiceClient(new HttpClient(new MockHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://material.test")
        });
    }

    private static HttpResponseMessage JsonResponse(MaterialServiceMaterialDto material)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(material)
        };
    }

    private static MaterialServiceMaterialDto CreateServiceMaterial(
        string name = "PEEK",
        string code = "PEEK",
        decimal pricePerUnit = 0m,
        int stockLevel = 0)
    {
        return new MaterialServiceMaterialDto
        {
            Id = Guid.Parse("2d281051-613d-b956-a625-af17a8536a0d"),
            Name = name,
            Code = code,
            Description = "High-performance polymer.",
            PricePerUnit = pricePerUnit,
            StockLevel = stockLevel,
            ManufacturingProcesses =
            [
                new() { Id = Guid.NewGuid(), Name = "CNC Milling" },
                new() { Id = Guid.NewGuid(), Name = "CNC Turning" }
            ],
            AvailableColors =
            [
                new() { Id = Guid.NewGuid(), Name = "Natural", HexCode = "#d9c7a4" },
                new() { Id = Guid.NewGuid(), Name = "Black", HexCode = "#171717" }
            ],
            PostProcessingMethods =
            [
                new() { Id = Guid.NewGuid(), Name = "As-machined" }
            ],
            MechanicalProperties =
            [
                new() { MechanicalPropertyId = Guid.NewGuid(), MechanicalPropertyName = "Tensile Strength", Value = 90m, Unit = "MPa" },
                new() { MechanicalPropertyId = Guid.NewGuid(), MechanicalPropertyName = "Chemical Resistance", Value = 5m, Unit = "1-5" }
            ],
            CreatedAt = new DateTimeOffset(2026, 5, 7, 0, 0, 0, TimeSpan.Zero),
            Active = true
        };
    }

    private sealed record CapturedMaterialUpdate
    {
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal PricePerUnit { get; init; }
        public int StockLevel { get; init; }
        public List<Guid> ManufacturingProcessIds { get; init; } = [];
        public List<Guid> ColorIds { get; init; } = [];
        public List<Guid> PostProcessingMethodIds { get; init; } = [];
        public List<CapturedMechanicalProperty> MechanicalProperties { get; init; } = [];
    }

    private sealed record CapturedMaterialCreate
    {
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal PricePerUnit { get; init; }
        public int StockLevel { get; init; }
        public List<Guid> ManufacturingProcessIds { get; init; } = [];
        public List<Guid> ColorIds { get; init; } = [];
        public List<Guid> PostProcessingMethodIds { get; init; } = [];
        public List<CapturedMechanicalProperty> MechanicalProperties { get; init; } = [];
    }

    private sealed record CapturedMechanicalProperty
    {
        public Guid MechanicalPropertyId { get; init; }
        public decimal Value { get; init; }
    }
}

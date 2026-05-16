using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

/// <summary>
/// Unit tests for <see cref="ProjectServiceClient.CreateProjectAsync"/>.
/// Verifies that success responses are deserialized correctly and that
/// error responses surface the raw error content instead of returning null silently.
/// </summary>
public class ProjectServiceClientCreateTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ProjectServiceClient MakeClient(
        HttpStatusCode status,
        string? responseBody = null)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = responseBody != null
                    ? new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
                    : new StringContent(string.Empty)
            }));
        return new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static CreateProjectRequest AnyRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        Title = "Test Project"
    };

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProjectAsync_WhenCreated_ReturnsProjectAndNoError()
    {
        var dto = new ProjectDetailDto { Id = Guid.NewGuid(), ProjectNumber = "PRJ-001" };
        var client = MakeClient(HttpStatusCode.OK, System.Text.Json.JsonSerializer.Serialize(dto));

        var (result, error, _) = await client.CreateProjectAsync(AnyRequest());

        Assert.NotNull(result);
        Assert.Equal("PRJ-001", result.ProjectNumber);
        Assert.Null(error);
    }

    [Fact]
    public async Task CreateProjectAsync_WhenCreated201_ReturnsProjectAndNoError()
    {
        var dto = new ProjectDetailDto { Id = Guid.NewGuid(), ProjectNumber = "PRJ-002" };
        var client = MakeClient(HttpStatusCode.Created, System.Text.Json.JsonSerializer.Serialize(dto));

        var (result, error, _) = await client.CreateProjectAsync(AnyRequest());

        Assert.NotNull(result);
        Assert.Null(error);
    }

    [Fact]
    public async Task GetProjectsAsync_ForwardsSearchAsQueryParameter()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    items = Array.Empty<object>(),
                    page = 3,
                    pageSize = 25,
                    totalCount = 0,
                    totalPages = 0
                })
            });
        });
        var client = new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        await client.GetProjectsAsync("Configuring", "fixture", Guid.Parse("11111111-1111-1111-1111-111111111111"), 3, 25);

        Assert.NotNull(capturedRequest);
        Assert.Equal(
            "/project/v1/projects?page=3&pageSize=25&status=Configuring&query=fixture&customerId=11111111-1111-1111-1111-111111111111",
            capturedRequest.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task GetProjectByIdAsync_WhenProjectServiceShape_ReturnsMappedIntranetDto()
    {
        var projectId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var quotationId = Guid.NewGuid();
        var responseBody = $$"""
        {
          "id": "{{projectId}}",
          "projectNumber": "PRJ-2026-0001",
          "customerId": "{{customerId}}",
          "customerName": "Axion Robotics",
          "title": "Fixture",
          "status": "QuotationGenerated",
          "quotationId": "{{quotationId}}",
          "quotationNumber": "Q-ABCD1234",
          "totalEstimatedPrice": 2500,
          "currency": "THB",
          "validUntil": "2026-06-02T00:00:00Z",
          "createdAt": "2026-05-03T04:30:00Z",
          "updatedAt": "2026-05-03T05:30:00Z",
          "parts": [
            {
              "id": "{{partId}}",
              "fileId": "{{fileId}}",
              "fileReference": "projects/fixture.stl",
              "fileName": "fixture.stl",
              "thumbnailUrl": "https://storage.example/thumb.png",
              "processType": "FDM",
              "materialId": "{{materialId}}",
              "materialName": "PLA",
              "materialCode": "PLA-BLK",
              "quantity": 2,
              "finishType": "FDM_STD",
              "customNotes": "Deburr all edges before anodizing.",
              "aiSuggestedPrice": 1200,
              "confirmedUnitPrice": 1250,
              "dfmAcknowledged": true,
              "hasDfmWarnings": true,
              "boundingBoxX": 80,
              "boundingBoxY": 149,
              "boundingBoxZ": 5,
              "isManifold": true,
              "status": "Confirmed"
            }
          ]
        }
        """;
        var client = MakeClient(HttpStatusCode.OK, responseBody);

        var result = await client.GetProjectByIdAsync(projectId);

        Assert.NotNull(result);
        Assert.Equal(2500m, result.TotalPrice);
        Assert.Equal("Generated", result.QuotationStatus);
        Assert.NotEmpty(result.Timeline);
        var part = Assert.Single(result.Parts);
        Assert.Equal("projects/fixture.stl", part.FileReference);
        Assert.Equal("PLA", part.MaterialName);
        Assert.Equal("PLA-BLK", part.MaterialCode);
        Assert.Equal("FDM_STD", part.Finish);
        Assert.Equal("Deburr all edges before anodizing.", part.PartNotes);
        Assert.Equal(1200m, part.EstimatedPrice);
        Assert.Equal(1250m, part.ConfirmedPrice);
        Assert.Equal(1250m, part.ConfirmedUnitPrice);
        Assert.Equal("https://storage.example/thumb.png", part.ModelPreviewUrl);
        Assert.Equal(80d, part.Dimensions?.X);
        Assert.True(part.IsManifold);
        Assert.True(part.DfmAcknowledged);
        Assert.True(part.HasDfmWarnings);
    }

    [Fact]
    public async Task AddNoteAsync_PostsToProjectServiceNotesEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        string? body = null;
        var projectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            capturedRequest = request;
            body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ProjectNoteDto
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    ProjectId = projectId,
                    AuthorName = "Alex Kim",
                    Content = "Review customer drawing.",
                    CreatedAt = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)
                })
            };
        });
        var client = new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var (result, error, statusCode) = await client.AddNoteAsync(projectId, new AddProjectNoteRequest { Content = "Review customer drawing." });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/project/v1/projects/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/notes", capturedRequest.RequestUri!.PathAndQuery);
        Assert.Contains("\"content\":\"Review customer drawing.\"", body);
        Assert.NotNull(result);
        Assert.Null(error);
        Assert.Equal(200, statusCode);
    }

    [Fact]
    public async Task GenerateQuotationAsync_PostsValidityAndDeliveryExpectations()
    {
        string? body = null;
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        await client.GenerateQuotationAsync(
            Guid.NewGuid(),
            new GenerateQuotationRequest
            {
                ValidityDays = 45,
                DeliveryExpectations = "Standard lead time"
            });

        Assert.NotNull(body);
        using var json = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal(45, json.RootElement.GetProperty("validityDays").GetInt32());
        Assert.Equal("Standard lead time", json.RootElement.GetProperty("deliveryExpectations").GetString());
    }

    [Fact]
    public async Task AddPartAsync_PostsMaterialAndGeometryContract()
    {
        string? body = null;
        var projectId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new
                {
                    id = Guid.NewGuid(),
                    fileName = "fixture.stl",
                    processType = "FDM",
                    materialId,
                    materialName = "PLA",
                    materialCode = "PLA-BLK",
                    quantity = 2,
                    aiSuggestedPrice = 120m
                })
            };
        });
        var client = new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        await client.AddPartAsync(projectId, new AddProjectPartRequest
        {
            FileName = "fixture.stl",
            ProcessType = "FDM",
            MaterialId = materialId,
            MaterialName = "PLA",
            MaterialCode = "PLA-BLK",
            Quantity = 2,
            VolumeCm3 = 12.5m,
            BoundingBoxX = 80m,
            BoundingBoxY = 149m,
            BoundingBoxZ = 5m,
            IsManifold = true,
            HasDfmWarnings = true,
            DfmAcknowledged = false,
            PartNotes = "Deburr all edges before anodizing."
        });

        Assert.NotNull(body);
        using var json = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("PLA", json.RootElement.GetProperty("materialName").GetString());
        Assert.Equal("PLA-BLK", json.RootElement.GetProperty("materialCode").GetString());
        Assert.Equal(12.5m, json.RootElement.GetProperty("volumeCm3").GetDecimal());
        Assert.Equal(80m, json.RootElement.GetProperty("boundingBoxX").GetDecimal());
        Assert.Equal("Deburr all edges before anodizing.", json.RootElement.GetProperty("customNotes").GetString());
        Assert.True(json.RootElement.GetProperty("isManifold").GetBoolean());
        Assert.True(json.RootElement.GetProperty("hasDfmWarnings").GetBoolean());
        Assert.False(json.RootElement.GetProperty("dfmAcknowledged").GetBoolean());
    }

    [Fact]
    public async Task UpdatePartAsync_ForwardsDfmWarningState()
    {
        string? body = null;
        var projectId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var materialId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler(async (request, ct) =>
        {
            body = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var response = await client.UpdatePartAsync(projectId, partId, new UpdateProjectPartRequest
        {
            ProcessType = "CNC_MILL",
            MaterialId = materialId,
            MaterialName = "Aluminium 6061-T6",
            MaterialCode = "AL6061-T6",
            Quantity = 4,
            DfmAcknowledged = true,
            HasDfmWarnings = true
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(body);
        using var json = System.Text.Json.JsonDocument.Parse(body);
        Assert.True(json.RootElement.GetProperty("hasDfmWarnings").GetBoolean());
        Assert.True(json.RootElement.GetProperty("dfmAcknowledged").GetBoolean());
    }

    // ── Error path — error content surfaced ───────────────────────────────────

    [Fact]
    public async Task CreateProjectAsync_When400_ReturnsNullResultAndErrorContent()
    {
        const string errorBody = """{"error":"CustomerId is required"}""";
        var client = MakeClient(HttpStatusCode.BadRequest, errorBody);

        var (result, error, statusCode) = await client.CreateProjectAsync(AnyRequest());

        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("CustomerId is required", error);
        Assert.Equal(400, statusCode);
    }

    [Fact]
    public async Task CreateProjectAsync_When500_ReturnsNullResultAndErrorContent()
    {
        const string errorBody = """{"error":"Internal server error"}""";
        var client = MakeClient(HttpStatusCode.InternalServerError, errorBody);

        var (result, error, statusCode) = await client.CreateProjectAsync(AnyRequest());

        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("Internal server error", error);
        Assert.Equal(500, statusCode);
    }

    [Fact]
    public async Task CreateProjectAsync_WhenEmptyErrorBody_ReturnsEmptyStringError()
    {
        var client = MakeClient(HttpStatusCode.BadRequest, string.Empty);

        var (result, error, statusCode) = await client.CreateProjectAsync(AnyRequest());

        Assert.Null(result);
        Assert.NotNull(error);  // empty string, not null
        Assert.Equal(400, statusCode);
    }

    [Fact]
    public async Task CreateProjectAsync_When403_ReturnsNullResultAndErrorContent()
    {
        const string errorBody = """{"error":"Forbidden"}""";
        var client = MakeClient(HttpStatusCode.Forbidden, errorBody);

        var (result, error, statusCode) = await client.CreateProjectAsync(AnyRequest());

        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Equal(403, statusCode);
    }

    [Fact]
    public async Task CreateProjectAsync_When502_ReturnsNullResultAndErrorContent()
    {
        const string errorBody = "Bad Gateway";
        var client = MakeClient(HttpStatusCode.BadGateway, errorBody);

        var (result, error, statusCode) = await client.CreateProjectAsync(AnyRequest());

        Assert.Null(result);
        Assert.Equal("Bad Gateway", error);
        Assert.Equal(502, statusCode);
    }
}

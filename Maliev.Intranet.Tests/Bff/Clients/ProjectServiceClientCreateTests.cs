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

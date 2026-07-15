using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class IAMServiceClientTests
{
    [Fact]
    public async Task GetPrincipalAsync_UsesSinglePrincipalEndpoint()
    {
        var principalId = Guid.Parse("51e818a9-680f-4332-8bab-293fe92e620d");
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri!.PathAndQuery == $"/iam/v1/principals/{principalId}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    principalId,
                    principalType = "user",
                    email = "natthapol@maliev.com",
                    displayName = "Natthapol Vanasrivilai",
                    linkedService = (string?)null,
                    linkedEntityId = (Guid?)null,
                    isActive = true,
                    createdAt = new DateTime(2026, 5, 4, 5, 48, 0, DateTimeKind.Utc),
                    updatedAt = new DateTime(2026, 5, 4, 5, 48, 0, DateTimeKind.Utc)
                })
            });

        var client = new IAMServiceClient(new HttpClient(handler.Object) { BaseAddress = new Uri("http://iam") });

        var result = await client.GetPrincipalAsync(principalId);

        Assert.NotNull(result);
        Assert.Equal(principalId, result.PrincipalId);
        Assert.Equal("Natthapol Vanasrivilai", result.DisplayName);
        Assert.Equal("natthapol@maliev.com", result.Email);
    }

    [Fact]
    public async Task GetRolesAsync_HumanizesRoleIdWhenIamRoleHasNoName()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri!.PathAndQuery == "/iam/v1/roles"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new
                    {
                        roleId = "roles.contact.viewer",
                        serviceName = "contact",
                        description = "Read-only access to contacts",
                        permissionIds = new[] { "contact.contacts.read", "contact.people.read", "contact.notes.read" }
                    }
                })
            });

        var client = new IAMServiceClient(new HttpClient(handler.Object) { BaseAddress = new Uri("http://iam") });

        var roles = await client.GetRolesAsync();

        var role = Assert.Single(roles);
        Assert.Equal("Contact Viewer", role.Name);
        Assert.Equal("contact", role.ServiceName);
        Assert.Equal("Read-only access to contacts", role.Description);
        Assert.Equal(3, role.PermissionIds.Count);
    }

    [Fact]
    public async Task GrantRoleAsync_UsesCanonicalBindingRouteAndRequiredRoleId()
    {
        var principalId = Guid.Parse("e7d25f00-c7e5-4e2a-9fa9-f9b02fb3d6c4");
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK);
        var client = new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://iam") });

        var result = await client.GrantRoleAsync(
            principalId,
            new GrantRoleRequestDto { RoleId = "roles.platform.viewer" });

        Assert.True(result);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal($"/iam/v1/principals/{principalId}/roles", handler.RequestUri?.PathAndQuery);
        using var document = JsonDocument.Parse(Assert.IsType<string>(handler.Body));
        Assert.Equal("roles.platform.viewer", document.RootElement.GetProperty("roleId").GetString());
        Assert.False(document.RootElement.TryGetProperty("userId", out _));
        Assert.False(document.RootElement.TryGetProperty("roles", out _));
        Assert.False(document.RootElement.TryGetProperty("permissions", out _));
    }

    [Fact]
    public async Task RevokeRoleAsync_UsesCanonicalBindingRouteWithoutPayload()
    {
        var principalId = Guid.Parse("09d9c8c8-faa5-4ef7-99e0-d1fba93f83a5");
        var bindingId = Guid.Parse("7d8f4485-b37e-41a9-9496-18d4601158e2");
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.NoContent);
        var client = new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://iam") });

        var result = await client.RevokeRoleAsync(principalId, bindingId);

        Assert.True(result);
        Assert.Equal(HttpMethod.Delete, handler.Method);
        Assert.Equal(
            $"/iam/v1/principals/{principalId}/roles/{bindingId}",
            handler.RequestUri?.PathAndQuery);
        Assert.Null(handler.Body);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_PreservesTheCompleteIamResponse()
    {
        var principalId = Guid.Parse("b4a8830f-bb19-48f1-9fd4-9ed52ff7175d");
        var cacheUntil = new DateTime(2026, 7, 15, 9, 30, 0, DateTimeKind.Utc);
        var handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(new
            {
                principalId,
                permissions = new[] { "system.diagnostics.read", "iam.permissions.list" },
                roles = new[] { "roles.system.diagnostics" },
                resourcePath = "projects/project-42",
                cacheUntil,
                fromCache = true
            }));
        var client = new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://iam") });

        var result = await client.ResolvePermissionsAsync(principalId.ToString());

        Assert.NotNull(result);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/iam/v1/auth/resolve-permissions", handler.RequestUri?.PathAndQuery);
        using var request = JsonDocument.Parse(Assert.IsType<string>(handler.Body));
        var property = Assert.Single(request.RootElement.EnumerateObject());
        Assert.Equal("principalId", property.Name);
        Assert.Equal(principalId.ToString(), property.Value.GetString());
        Assert.Equal(principalId, result.PrincipalId);
        Assert.Equal(["system.diagnostics.read", "iam.permissions.list"], result.Permissions);
        Assert.Equal(["roles.system.diagnostics"], result.Roles);
        Assert.Equal("projects/project-42", result.ResourcePath);
        Assert.Equal(cacheUntil, result.CacheUntil);
        Assert.True(result.FromCache);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_DoesNotTreatAnEmptyPrincipalIdAsANullResponse()
    {
        var handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(new
            {
                principalId = Guid.Empty,
                permissions = Array.Empty<string>(),
                roles = Array.Empty<string>(),
                resourcePath = (string?)null,
                cacheUntil = (DateTime?)null,
                fromCache = false
            }));
        var client = new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://iam") });

        var result = await client.ResolvePermissionsAsync(Guid.Empty.ToString());

        Assert.NotNull(result);
        Assert.Equal(Guid.Empty, result.PrincipalId);
        Assert.Empty(result.Permissions);
        Assert.Empty(result.Roles);
        Assert.Null(result.ResourcePath);
        Assert.Null(result.CacheUntil);
        Assert.False(result.FromCache);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_RejectsANullSuccessBody()
    {
        var handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create<object?>(null));
        var client = new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://iam") });

        await Assert.ThrowsAsync<JsonException>(() => client.ResolvePermissionsAsync("principal"));
    }

    [Theory]
    [InlineData("permissions")]
    [InlineData("roles")]
    public async Task ResolvePermissionsAsync_RejectsNullRequiredCollections(string nullProperty)
    {
        var permissions = nullProperty == "permissions" ? "null" : "[]";
        var roles = nullProperty == "roles" ? "null" : "[]";
        var json = $$"""
            {
              "principalId": "b4a8830f-bb19-48f1-9fd4-9ed52ff7175d",
              "permissions": {{permissions}},
              "roles": {{roles}},
              "resourcePath": null,
              "cacheUntil": null,
              "fromCache": false
            }
            """;
        var handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        var client = new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://iam") });

        var exception = await Assert.ThrowsAsync<JsonException>(
            () => client.ResolvePermissionsAsync("principal"));

        Assert.Contains(nullProperty, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolvePermissionsAsync_PropagatesCancellationToIam()
    {
        var handler = new CancellationObservingHttpMessageHandler();
        var client = new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://iam") });
        using var cancellation = new CancellationTokenSource();

        var pending = client.ResolvePermissionsAsync("principal", cancellation.Token);
        await handler.RequestStarted;
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.True(handler.CancellationObserved);
    }

    private sealed class RecordingHttpMessageHandler(
        HttpStatusCode statusCode,
        HttpContent? responseContent = null) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode) { Content = responseContent };
        }
    }

    private sealed class CancellationObservingHttpMessageHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _requestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task RequestStarted => _requestStarted.Task;

        public bool CancellationObserved { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _requestStarted.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("The request should have been cancelled.");
            }
            catch (OperationCanceledException)
            {
                CancellationObserved = true;
                throw;
            }
        }
    }
}

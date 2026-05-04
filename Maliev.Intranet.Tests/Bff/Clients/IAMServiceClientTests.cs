using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
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
}

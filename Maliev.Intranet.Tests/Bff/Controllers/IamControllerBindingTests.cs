using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class IamControllerBindingTests
{
    [Fact]
    public async Task GrantRole_ReturnsBindingAndPropagatesCancellation()
    {
        var principalId = Guid.NewGuid();
        var request = new GrantRoleRequestDto { RoleId = "roles.iam.viewer" };
        var binding = new RoleBindingDto
        {
            BindingId = Guid.NewGuid(),
            PrincipalId = principalId,
            RoleId = request.RoleId,
            GrantedAt = DateTime.UtcNow
        };
        using var cancellation = new CancellationTokenSource();
        var iamClient = CreateIamClient();
        iamClient
            .Setup(client => client.GrantRoleAsync(principalId, request, cancellation.Token))
            .ReturnsAsync(binding);
        var controller = CreateController(iamClient);

        var result = await controller.GrantRole(principalId, request, cancellation.Token);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(binding, ok.Value);
        iamClient.Verify(client => client.GrantRoleAsync(principalId, request, cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(HttpStatusCode.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(HttpStatusCode.BadRequest, StatusCodes.Status400BadRequest)]
    [InlineData(HttpStatusCode.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(HttpStatusCode.InternalServerError, StatusCodes.Status503ServiceUnavailable)]
    public async Task GrantRole_MapsIamFailureStatus(HttpStatusCode downstreamStatus, int expectedStatus)
    {
        var principalId = Guid.NewGuid();
        var request = new GrantRoleRequestDto { RoleId = "roles.iam.viewer" };
        var iamClient = CreateIamClient();
        iamClient
            .Setup(client => client.GrantRoleAsync(principalId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("IAM binding failed.", null, downstreamStatus));
        var controller = CreateController(iamClient);

        var result = await controller.GrantRole(principalId, request, CancellationToken.None);

        Assert.Equal(expectedStatus, GetStatusCode(result));
    }

    [Fact]
    public async Task GrantRole_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var iamClient = CreateIamClient();
        iamClient
            .Setup(client => client.GrantRoleAsync(It.IsAny<Guid>(), It.IsAny<GrantRoleRequestDto>(), cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        var controller = CreateController(iamClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => controller.GrantRole(
            Guid.NewGuid(),
            new GrantRoleRequestDto { RoleId = "roles.iam.viewer" },
            cancellation.Token));
    }

    [Fact]
    public async Task GetUserRoles_PropagatesCancellationToClient()
    {
        var principalId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var iamClient = CreateIamClient();
        iamClient
            .Setup(client => client.GetPrincipalRolesAsync(principalId, cancellation.Token))
            .ReturnsAsync([]);
        var controller = CreateController(iamClient);

        await controller.GetUserRoles(principalId, cancellation.Token);

        iamClient.Verify(client => client.GetPrincipalRolesAsync(principalId, cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{not-json")]
    [InlineData("[{}]")]
    public async Task GetUserRoles_InvalidIamSuccessContract_ReturnsServiceUnavailable(string body)
    {
        var iamClient = CreateRawIamClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        }));
        var controller = CreateController(iamClient);

        var result = await controller.GetUserRoles(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GetStatusCode(result));
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(HttpStatusCode.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(HttpStatusCode.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(HttpStatusCode.ServiceUnavailable, StatusCodes.Status503ServiceUnavailable)]
    public async Task RevokeRole_MapsIamFailureStatus(HttpStatusCode downstreamStatus, int expectedStatus)
    {
        var principalId = Guid.NewGuid();
        var bindingId = Guid.NewGuid();
        var iamClient = CreateIamClient();
        iamClient
            .Setup(client => client.RevokeRoleAsync(principalId, bindingId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("IAM binding revoke failed.", null, downstreamStatus));
        var controller = CreateController(iamClient);

        var result = await controller.RevokeRole(principalId, bindingId, CancellationToken.None);

        Assert.Equal(expectedStatus, GetStatusCode(result));
    }

    [Fact]
    public async Task RevokeRole_NonCallerCancellation_ReturnsServiceUnavailable()
    {
        var iamClient = CreateRawIamClient((_, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("IAM revoke timed out.")));
        var controller = CreateController(iamClient);

        var result = await controller.RevokeRole(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GetStatusCode(result));
    }

    [Fact]
    public async Task RevokeRole_CallerCancellationStillPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        var iamClient = CreateRawIamClient((_, _) =>
        {
            cancellation.Cancel();
            return Task.FromException<HttpResponseMessage>(new OperationCanceledException(cancellation.Token));
        });
        var controller = CreateController(iamClient);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.RevokeRole(Guid.NewGuid(), Guid.NewGuid(), cancellation.Token));
    }

    [Fact]
    public async Task InviteUser_DoesNotReportSuccessWhenBindingConflicts()
    {
        var principal = new PrincipalSummaryDto
        {
            PrincipalId = Guid.NewGuid(),
            Email = "new.employee@maliev.com",
            DisplayName = "New Employee"
        };
        var iamClient = CreateIamClient();
        iamClient
            .Setup(client => client.CreatePrincipalAsync(principal.Email, principal.DisplayName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);
        iamClient
            .Setup(client => client.GrantRoleAsync(principal.PrincipalId, It.IsAny<GrantRoleRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Binding already exists.", null, HttpStatusCode.Conflict));
        var controller = CreateController(iamClient, authorizeBinding: true);

        var result = await controller.InviteUser(
            new IamController.InviteUserRequest
            {
                Email = principal.Email,
                DisplayName = principal.DisplayName,
                RoleId = "roles.iam.viewer"
            },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status409Conflict, GetStatusCode(result));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{not-json")]
    public async Task GrantRole_InvalidIamSuccessBody_ReturnsServiceUnavailable(string body)
    {
        var iamClient = CreateRawIamClient((request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        }));
        var controller = CreateController(iamClient);

        var result = await controller.GrantRole(
            Guid.NewGuid(),
            new GrantRoleRequestDto { RoleId = "roles.iam.viewer" },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GetStatusCode(result));
    }

    [Fact]
    public async Task GrantRole_NonCallerCancellation_ReturnsServiceUnavailable()
    {
        var iamClient = CreateRawIamClient((_, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("IAM request timed out.")));
        var controller = CreateController(iamClient);

        var result = await controller.GrantRole(
            Guid.NewGuid(),
            new GrantRoleRequestDto { RoleId = "roles.iam.viewer" },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GetStatusCode(result));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{not-json")]
    public async Task InviteUser_InvalidIamSuccessBody_ReturnsServiceUnavailable(string body)
    {
        var principalId = Guid.NewGuid();
        var iamClient = CreateRawIamClient((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/iam/v1/principals")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(new { principalId, createdAt = DateTime.UtcNow })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        });
        var controller = CreateController(iamClient, authorizeBinding: true);

        var result = await controller.InviteUser(CreateInviteRequest(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GetStatusCode(result));
    }

    [Fact]
    public async Task InviteUser_NonCallerCancellation_ReturnsServiceUnavailable()
    {
        var principalId = Guid.NewGuid();
        var iamClient = CreateRawIamClient((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/iam/v1/principals")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(new { principalId, createdAt = DateTime.UtcNow })
                });
            }

            return Task.FromException<HttpResponseMessage>(new TaskCanceledException("IAM request timed out."));
        });
        var controller = CreateController(iamClient, authorizeBinding: true);

        var result = await controller.InviteUser(CreateInviteRequest(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GetStatusCode(result));
    }

    [Fact]
    public async Task InviteUser_CallerCancellationStillPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        var principal = new PrincipalSummaryDto
        {
            PrincipalId = Guid.NewGuid(),
            Email = "new.employee@maliev.com",
            DisplayName = "New Employee"
        };
        var iamClient = CreateIamClient();
        iamClient
            .Setup(client => client.CreatePrincipalAsync(principal.Email, principal.DisplayName, cancellation.Token))
            .ReturnsAsync(principal);
        iamClient
            .Setup(client => client.GrantRoleAsync(principal.PrincipalId, It.IsAny<GrantRoleRequestDto>(), cancellation.Token))
            .Callback(cancellation.Cancel)
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        var controller = CreateController(iamClient, authorizeBinding: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.InviteUser(CreateInviteRequest(), cancellation.Token));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""
        {
          "bindingId": "8cc18a23-8476-4dbb-82c1-11998075c8f5",
          "principalId": "11111111-1111-1111-1111-111111111111",
          "roleId": "roles.iam.viewer",
          "grantedAt": "2026-07-15T03:00:00Z"
        }
        """)]
    [InlineData("""
        {
          "bindingId": "8cc18a23-8476-4dbb-82c1-11998075c8f5",
          "principalId": "e7d25f00-c7e5-4e2a-9fa9-f9b02fb3d6c4",
          "roleId": "roles.iam.admin",
          "grantedAt": "2026-07-15T03:00:00Z"
        }
        """)]
    public async Task GrantRole_ContractInvalidIamSuccessBody_ReturnsServiceUnavailable(string body)
    {
        var principalId = Guid.Parse("e7d25f00-c7e5-4e2a-9fa9-f9b02fb3d6c4");
        var iamClient = CreateRawIamClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        }));
        var controller = CreateController(iamClient);

        var result = await controller.GrantRole(
            principalId,
            new GrantRoleRequestDto { RoleId = "roles.iam.viewer" },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GetStatusCode(result));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""
        {
          "bindingId": "63c90380-62ed-4d2a-aa4a-591bb86d2625",
          "principalId": "11111111-1111-1111-1111-111111111111",
          "roleId": "roles.iam.viewer",
          "grantedAt": "2026-07-15T03:00:00Z"
        }
        """)]
    [InlineData("""
        {
          "bindingId": "63c90380-62ed-4d2a-aa4a-591bb86d2625",
          "principalId": "a1f0d442-d541-42e8-8fcc-8e513ecb8fc3",
          "roleId": "roles.iam.admin",
          "grantedAt": "2026-07-15T03:00:00Z"
        }
        """)]
    public async Task InviteUser_ContractInvalidIamSuccessBody_ReturnsServiceUnavailable(string body)
    {
        var principalId = Guid.Parse("a1f0d442-d541-42e8-8fcc-8e513ecb8fc3");
        var iamClient = CreateRawIamClient((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/iam/v1/principals")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(new { principalId, createdAt = DateTime.UtcNow })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        });
        var controller = CreateController(iamClient, authorizeBinding: true);

        var result = await controller.InviteUser(CreateInviteRequest(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GetStatusCode(result));
    }

    private static Mock<IAMServiceClient> CreateIamClient() =>
        new(new HttpClient { BaseAddress = new Uri("http://iam") });

    private static IAMServiceClient CreateRawIamClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) =>
        new(new HttpClient(new StubHttpMessageHandler(responder)) { BaseAddress = new Uri("http://iam") });

    private static IamController CreateController(Mock<IAMServiceClient> iamClient, bool authorizeBinding = false)
        => CreateController(iamClient.Object, authorizeBinding);

    private static IamController CreateController(IAMServiceClient iamClient, bool authorizeBinding = false)
    {
        var authorization = new Mock<IAuthorizationService>();
        authorization
            .Setup(service => service.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(authorizeBinding ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        return new IamController(iamClient, authorization.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "Test"))
                }
            }
        };
    }

    private static IamController.InviteUserRequest CreateInviteRequest() => new()
    {
        Email = "new.employee@maliev.com",
        DisplayName = "New Employee",
        RoleId = "roles.iam.viewer"
    };

    private static int GetStatusCode(IActionResult result) => result switch
    {
        ForbidResult => StatusCodes.Status403Forbidden,
        UnauthorizedResult => StatusCodes.Status401Unauthorized,
        StatusCodeResult status => status.StatusCode,
        ObjectResult status => status.StatusCode ?? StatusCodes.Status200OK,
        _ => throw new Xunit.Sdk.XunitException($"Unexpected result type {result.GetType().Name}.")
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responder(request, cancellationToken);
    }
}

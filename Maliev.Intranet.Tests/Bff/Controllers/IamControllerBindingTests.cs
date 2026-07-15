using System.Net;
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

    private static Mock<IAMServiceClient> CreateIamClient() =>
        new(new HttpClient { BaseAddress = new Uri("http://iam") });

    private static IamController CreateController(Mock<IAMServiceClient> iamClient, bool authorizeBinding = false)
    {
        var authorization = new Mock<IAuthorizationService>();
        authorization
            .Setup(service => service.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(authorizeBinding ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        return new IamController(iamClient.Object, authorization.Object)
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

    private static int GetStatusCode(IActionResult result) => result switch
    {
        ForbidResult => StatusCodes.Status403Forbidden,
        UnauthorizedResult => StatusCodes.Status401Unauthorized,
        StatusCodeResult status => status.StatusCode,
        ObjectResult status => status.StatusCode ?? StatusCodes.Status200OK,
        _ => throw new Xunit.Sdk.XunitException($"Unexpected result type {result.GetType().Name}.")
    };
}

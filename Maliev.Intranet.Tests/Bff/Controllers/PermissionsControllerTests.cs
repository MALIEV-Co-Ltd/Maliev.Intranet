using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class PermissionsControllerTests
{
    private static IAMServiceClient CreateClient<T>(T response)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) }));
        return new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task GetAvailablePermissions_ReturnsOk()
    {
        var controller = new PermissionsController(CreateClient(new List<PermissionDto>()));
        var result = await controller.GetAvailablePermissions();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAvailableRoles_ReturnsOk()
    {
        var controller = new PermissionsController(CreateClient(new List<RoleDto>()));
        var result = await controller.GetAvailableRoles();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetUserAssignments_WithInvalidGuid_ReturnsNotFound()
    {
        var controller = new PermissionsController(CreateClient(new UserContextDto()));
        // Non-Guid userId → IAMServiceClient.GetUserAssignmentsAsync returns null → NotFound
        var result = await controller.GetUserAssignments("not-a-guid");
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetUserAssignments_WithValidGuid_ReturnsOk()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new UserContextDto()) }));
        var client = new IAMServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new PermissionsController(client);
        var result = await controller.GetUserAssignments(Guid.NewGuid().ToString());
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAssignments_WithInvalidUserId_ReturnsBadRequest()
    {
        var controller = new PermissionsController(CreateClient(new object()));
        // UserId is not a valid Guid → AssignToUserAsync returns false → BadRequest
        var result = await controller.UpdateAssignments(new UserAssignmentRequest { UserId = "invalid", Roles = [] });
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateAssignments_WithValidUserId_ReturnsOk()
    {
        var controller = new PermissionsController(CreateClient(new object()));
        // Valid Guid with empty roles → no HTTP calls → returns true → Ok
        var result = await controller.UpdateAssignments(new UserAssignmentRequest { UserId = Guid.NewGuid().ToString(), Roles = [] });
        Assert.IsType<OkResult>(result);
    }
}

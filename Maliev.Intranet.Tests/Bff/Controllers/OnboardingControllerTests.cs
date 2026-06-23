using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class OnboardingControllerTests
{
    private readonly Mock<ILifecycleServiceClient> _clientMock = new();

    [Fact]
    public async Task Get_WhenClientReturnsNull_ReturnsEmptyPagedResponse()
    {
        _clientMock.Setup(client => client.GetOnboardingsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResponse<OnboardingSummaryDto>?)null);
        var controller = new OnboardingController(_clientMock.Object);

        var result = await controller.Get(page: 1, pageSize: 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PagedResponse<OnboardingSummaryDto>>(ok.Value);
        Assert.Empty(dto.Data);
    }

    [Fact]
    public async Task GetChecklist_WhenChecklistMissing_ReturnsNotFound()
    {
        _clientMock.Setup(client => client.GetChecklistAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingChecklistDto?)null);
        var controller = new OnboardingController(_clientMock.Object);

        var result = await controller.GetChecklist(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTask_WhenClientUpdatesTask_ReturnsNoContent()
    {
        _clientMock.Setup(client => client.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateOnboardingProgressRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new OnboardingController(_clientMock.Object);

        var result = await controller.UpdateTask(Guid.NewGuid(), new UpdateOnboardingProgressRequest { TaskId = Guid.NewGuid(), IsCompleted = true }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateTask_WhenClientRejectsTask_ReturnsBadRequest()
    {
        _clientMock.Setup(client => client.UpdateTaskAsync(It.IsAny<Guid>(), It.IsAny<UpdateOnboardingProgressRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = new OnboardingController(_clientMock.Object);

        var result = await controller.UpdateTask(Guid.NewGuid(), new UpdateOnboardingProgressRequest { TaskId = Guid.NewGuid(), IsCompleted = false }, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Theory]
    [InlineData(nameof(OnboardingController.Get), MalievPermissions.Onboarding.Read)]
    [InlineData(nameof(OnboardingController.GetChecklist), MalievPermissions.Onboarding.Read)]
    [InlineData(nameof(OnboardingController.UpdateTask), MalievPermissions.Onboarding.Write)]
    public void OnboardingEndpoints_RequireOnboardingPermissions(string actionName, string expectedPermission)
    {
        var method = typeof(OnboardingController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task LifecycleServiceClient_MapsPendingOnboardingToPagedSummaries()
    {
        var onboardingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var employeeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var requestedPaths = new List<string>();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedPaths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new
                    {
                        id = onboardingId,
                        employeeId,
                        status = "InProgress",
                        startDate = DateTime.Parse("2026-06-23T08:00:00Z").ToUniversalTime(),
                        totalItems = 4,
                        completedItems = 3,
                        items = Array.Empty<object>()
                    }
                })
            });
        });
        var client = new LifecycleServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://lifecycle") });

        var result = await client.GetOnboardingsAsync(page: 2, pageSize: 10);

        Assert.NotNull(result);
        Assert.Equal("/lifecycle/v1/onboarding/pending?offset=10&limit=10", Assert.Single(requestedPaths));
        var summary = Assert.Single(result.Data);
        Assert.Equal(onboardingId, summary.Id);
        Assert.Equal(employeeId, summary.EmployeeId);
        Assert.Equal(75, summary.Progress);
        Assert.Equal("InProgress", summary.Status);
        Assert.Equal(1, result.Meta.TotalCount);
        Assert.Equal(2, result.Meta.CurrentPage);
    }

    [Fact]
    public async Task LifecycleServiceClient_MapsEmployeeChecklistTasks()
    {
        var onboardingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var employeeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var taskId = Guid.Parse("99999999-8888-7777-6666-555555555555");
        var assigneeId = Guid.Parse("22222222-3333-4444-5555-666666666666");
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            Assert.Equal($"/lifecycle/v1/employees/{employeeId}/onboarding/status", request.RequestUri?.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = onboardingId,
                    employeeId,
                    status = "InProgress",
                    startDate = DateTime.Parse("2026-06-23T08:00:00Z").ToUniversalTime(),
                    totalItems = 1,
                    completedItems = 0,
                    items = new[]
                    {
                        new
                        {
                            id = taskId,
                            title = "Set up workstation",
                            description = "Prepare laptop and access.",
                            assignedTo = assigneeId,
                            isCompleted = false
                        }
                    }
                })
            });
        });
        var client = new LifecycleServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://lifecycle") });

        var result = await client.GetChecklistAsync(employeeId);

        Assert.NotNull(result);
        Assert.Equal(onboardingId, result.Id);
        Assert.Equal(employeeId, result.EmployeeId);
        var task = Assert.Single(result.Tasks);
        Assert.Equal(taskId, task.Id);
        Assert.Equal("Set up workstation", task.Title);
        Assert.Equal("Prepare laptop and access.", task.Description);
        Assert.Equal(assigneeId, task.AssignedTo);
        Assert.False(task.IsCompleted);
    }

    [Fact]
    public async Task LifecycleServiceClient_UpdateTask_CompletesOnlyCompletedRequests()
    {
        var employeeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var taskId = Guid.Parse("99999999-8888-7777-6666-555555555555");
        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(async (request, _) =>
        {
            capturedRequest = request;
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"notes\"", body, StringComparison.OrdinalIgnoreCase);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = new LifecycleServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://lifecycle") });

        var notCompleted = await client.UpdateTaskAsync(employeeId, new UpdateOnboardingProgressRequest { TaskId = taskId, IsCompleted = false });
        var completed = await client.UpdateTaskAsync(employeeId, new UpdateOnboardingProgressRequest { TaskId = taskId, IsCompleted = true });

        Assert.False(notCompleted);
        Assert.True(completed);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Put, capturedRequest.Method);
        Assert.Equal($"/lifecycle/v1/onboarding-items/{taskId}/complete", capturedRequest.RequestUri?.PathAndQuery);
    }

    [Fact]
    public void ChatDrawer_ExposesOnboardingNavigationLabel()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "ChatDrawer.razor");

        Assert.Contains("[\"/hr/onboarding\"] = \"Onboarding\"", source, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        var startDirectories = new List<string>
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var startDirectory in startDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(startDirectory);
            while (current is not null)
            {
                var candidate = Path.Combine(new[] { current.FullName }.Concat(relativeParts).ToArray());
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                current = current.Parent;
            }
        }

        throw new FileNotFoundException($"Unable to locate {Path.Combine(relativeParts)} from {AppContext.BaseDirectory}.");
    }
}

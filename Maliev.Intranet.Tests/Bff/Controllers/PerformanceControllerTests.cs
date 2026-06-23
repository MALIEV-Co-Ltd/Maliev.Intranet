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

public class PerformanceControllerTests
{
    private readonly Mock<IPerformanceServiceClient> _clientMock = new();

    [Fact]
    public async Task GetReviews_WhenClientReturnsNull_ReturnsEmptyList()
    {
        _clientMock.Setup(client => client.GetReviewsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<PerformanceReviewDto>?)null);
        var controller = new PerformanceController(_clientMock.Object);

        var result = await controller.GetReviews(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<List<PerformanceReviewDto>>(ok.Value);
        Assert.Empty(dto);
    }

    [Fact]
    public async Task GetGoals_WhenClientReturnsNull_ReturnsEmptyList()
    {
        _clientMock.Setup(client => client.GetGoalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<GoalDto>?)null);
        var controller = new PerformanceController(_clientMock.Object);

        var result = await controller.GetGoals(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<List<GoalDto>>(ok.Value);
        Assert.Empty(dto);
    }

    [Theory]
    [InlineData(nameof(PerformanceController.GetReviews))]
    [InlineData(nameof(PerformanceController.GetGoals))]
    public void PerformanceEndpoints_RequirePerformanceReadPermission(string actionName)
    {
        var method = typeof(PerformanceController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(MalievPermissions.Performance.Read, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task PerformanceServiceClient_ReadsReviewsFromPerformanceService()
    {
        var reviewId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var employeeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var requestedPaths = new List<string>();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedPaths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new PerformanceReviewDto
                    {
                        Id = reviewId,
                        EmployeeId = employeeId,
                        ReviewerId = Guid.Parse("22222222-3333-4444-5555-666666666666"),
                        Cycle = "2026-H1",
                        Rating = 5,
                        Comments = "Strong delivery",
                        Status = "Completed",
                        ReviewerName = "Avery Lead"
                    }
                })
            });
        });
        var client = new PerformanceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://performance") });

        var result = await client.GetReviewsAsync();

        Assert.NotNull(result);
        Assert.Equal("/performance/v1/reviews", Assert.Single(requestedPaths));
        var review = Assert.Single(result);
        Assert.Equal(reviewId, review.Id);
        Assert.Equal(employeeId, review.EmployeeId);
        Assert.Equal("2026-H1", review.Cycle);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Completed", review.Status);
    }

    [Fact]
    public async Task PerformanceServiceClient_ReadsGoalsFromPerformanceService()
    {
        var goalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var employeeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var requestedPaths = new List<string>();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            requestedPaths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new GoalDto
                    {
                        Id = goalId,
                        EmployeeId = employeeId,
                        Title = "Improve quotation accuracy",
                        Description = "Reduce manual quote corrections.",
                        Status = "InProgress",
                        DueDate = DateTime.Parse("2026-12-31T00:00:00Z").ToUniversalTime(),
                        Progress = 40
                    }
                })
            });
        });
        var client = new PerformanceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://performance") });

        var result = await client.GetGoalsAsync();

        Assert.NotNull(result);
        Assert.Equal("/performance/v1/goals", Assert.Single(requestedPaths));
        var goal = Assert.Single(result);
        Assert.Equal(goalId, goal.Id);
        Assert.Equal(employeeId, goal.EmployeeId);
        Assert.Equal("Improve quotation accuracy", goal.Title);
        Assert.Equal("InProgress", goal.Status);
        Assert.Equal(40, goal.Progress);
    }

    [Fact]
    public void ChatDrawer_ExposesPerformanceNavigationLabel()
    {
        var source = ReadRepoFile("Maliev.Intranet.Client", "Components", "ChatDrawer.razor");

        Assert.Contains("[\"/hr/performance\"] = \"Performance\"", source, StringComparison.Ordinal);
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

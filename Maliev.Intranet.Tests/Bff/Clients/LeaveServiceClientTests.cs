using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class LeaveServiceClientTests
{
    private static LeaveServiceClient CreateClient<T>(T response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var httpResponse = new HttpResponseMessage(status) { Content = JsonContent.Create(response) };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        return new LeaveServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static LeaveServiceClient CreateRawClient(HttpStatusCode status = HttpStatusCode.OK)
    {
        var httpResponse = new HttpResponseMessage(status);
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        return new LeaveServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    [Fact]
    public async Task GetMyBalancesAsync_ShouldReturnListOfBalances()
    {
        var balances = new List<LeaveBalanceDto>
        {
            new() { LeaveType = "Annual", Entitlement = 15, Used = 3, Available = 12 },
            new() { LeaveType = "Sick", Entitlement = 30, Used = 0, Available = 30 }
        };
        var client = CreateClient(balances);

        var result = await client.GetMyBalancesAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Annual", result[0].LeaveType);
    }

    [Fact]
    public async Task GetMyBalancesAsync_ShouldReturnEmptyList_WhenDownstreamFails()
    {
        var client = CreateRawClient(HttpStatusCode.NotFound);

        var result = await client.GetMyBalancesAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyRequestsAsync_ShouldReturnListOfRequests()
    {
        var requests = new List<LeaveRequestSummaryDto>
        {
            new() { Id = Guid.NewGuid(), LeaveType = "Annual", Status = "Pending", Days = 2 },
            new() { Id = Guid.NewGuid(), LeaveType = "Sick", Status = "Approved", Days = 1 }
        };
        var client = CreateClient(requests);

        var result = await client.GetMyRequestsAsync(Guid.NewGuid(), null);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetMyRequestsAsync_WithYear_ShouldReturnFilteredRequests()
    {
        string? capturedUrl = null;
        var requests = new List<LeaveRequestSummaryDto>();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(requests) };
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedUrl = req.RequestUri?.ToString();
            return Task.FromResult(httpResponse);
        });
        var client = new LeaveServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        await client.GetMyRequestsAsync(Guid.NewGuid(), 2025);

        Assert.NotNull(capturedUrl);
        Assert.Contains("year=2025", capturedUrl);
    }

    [Fact]
    public async Task GetMyRequestsAsync_ShouldReturnEmptyList_WhenDownstreamFails()
    {
        var client = CreateRawClient(HttpStatusCode.ServiceUnavailable);

        var result = await client.GetMyRequestsAsync(Guid.NewGuid(), null);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SubmitRequestAsync_ShouldSendPostRequest_AndReturnCreatedRequest()
    {
        string? capturedMethod = null;
        var created = new LeaveRequestDetailDto
        {
            Id = Guid.NewGuid(),
            LeaveType = "Annual",
            Status = "Pending",
            Days = 3
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(created) };
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedMethod = req.Method.Method;
            return Task.FromResult(httpResponse);
        });
        var client = new LeaveServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var request = new SubmitLeaveRequestDto
        {
            LeaveType = "Annual",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(9),
            Reason = "Family holiday"
        };

        var result = await client.SubmitRequestAsync(Guid.NewGuid(), request);

        Assert.Equal("POST", capturedMethod);
        Assert.NotNull(result);
        Assert.Equal(created.LeaveType, result.LeaveType);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task SubmitRequestAsync_ShouldReturnNull_WhenDownstreamFails()
    {
        var client = CreateRawClient(HttpStatusCode.BadRequest);

        var request = new SubmitLeaveRequestDto
        {
            LeaveType = "Annual",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Reason = "Test"
        };

        var result = await client.SubmitRequestAsync(Guid.NewGuid(), request);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_ShouldReturnListOfPendingRequests()
    {
        var pending = new List<LeaveRequestDetailDto>
        {
            new() { Id = Guid.NewGuid(), LeaveType = "Annual", Status = "Pending" }
        };
        var client = CreateClient(pending);

        var result = await client.GetPendingApprovalsAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task ProcessDecisionAsync_ShouldReturnTrue_WhenSuccessful()
    {
        var client = CreateRawClient(HttpStatusCode.OK);

        var result = await client.ProcessDecisionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ApproveRejectLeaveRequest { Decision = "Approve" });

        Assert.True(result);
    }

    [Fact]
    public async Task ProcessDecisionAsync_ShouldReturnFalse_WhenDownstreamFails()
    {
        var client = CreateRawClient(HttpStatusCode.BadRequest);

        var result = await client.ProcessDecisionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ApproveRejectLeaveRequest { Decision = "Reject" });

        Assert.False(result);
    }
}

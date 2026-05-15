using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class LeaveServiceClientTests
{
    [Fact]
    public async Task GetMyRequestsAsync_MapsSnakeCaseEnumPayload()
    {
        var requestId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new
                    {
                        id = requestId,
                        employee_id = employeeId,
                        leave_type = 1,
                        start_date = "2026-06-01T00:00:00Z",
                        end_date = "2026-06-02T00:00:00Z",
                        total_days = 2m,
                        status = 2
                    }
                })
            }));
        var client = new LeaveServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://leave") });

        var requests = await client.GetMyRequestsAsync(employeeId, 2026);

        var request = Assert.Single(requests);
        Assert.Equal(requestId, request.Id);
        Assert.Equal("Annual", request.LeaveType);
        Assert.Equal("Approved", request.Status);
        Assert.Equal(2m, request.Days);
    }

    [Fact]
    public async Task SubmitRequestAsync_SendsSnakeCaseNumericPayloadWithTrustedApprover()
    {
        var employeeId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        string? requestBody = null;
        var handler = new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new { id = requestId })
            };
        });
        var client = new LeaveServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://leave") });

        var result = await client.SubmitRequestAsync(employeeId, new SubmitLeaveRequestDto
        {
            LeaveType = "Annual",
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 2),
            Reason = "Family appointment"
        }, approverId);

        Assert.NotNull(result);
        Assert.Equal(requestId, result.Id);
        Assert.Equal(employeeId, result.EmployeeId);
        Assert.Equal("Pending", result.Status);
        Assert.NotNull(requestBody);
        using var document = JsonDocument.Parse(requestBody);
        var payload = document.RootElement;
        Assert.Equal(1, payload.GetProperty("leave_type").GetInt32());
        Assert.Equal(0, payload.GetProperty("half_day_period").GetInt32());
        Assert.Equal(approverId, payload.GetProperty("approver_id").GetGuid());
    }
}

using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using System.Text.Json;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Leave microservice.
/// </summary>
public interface ILeaveServiceClient
{
    /// <summary>
    /// Retrieves leave balances for an employee.
    /// </summary>
    Task<List<LeaveBalanceDto>> GetMyBalancesAsync(Guid employeeId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves leave requests for an employee.
    /// </summary>
    Task<List<LeaveRequestSummaryDto>> GetMyRequestsAsync(Guid employeeId, int? year, CancellationToken ct = default);

    /// <summary>
    /// Submits a new leave request.
    /// </summary>
    Task<LeaveRequestDetailDto?> SubmitRequestAsync(Guid employeeId, SubmitLeaveRequestDto request, Guid? approverId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets pending approvals for a manager.
    /// </summary>
    Task<List<LeaveRequestDetailDto>> GetPendingApprovalsAsync(Guid managerId, CancellationToken ct = default);

    /// <summary>
    /// Approves or rejects a leave request.
    /// </summary>
    Task<bool> ProcessDecisionAsync(Guid requestId, Guid approverId, ApproveRejectLeaveRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the count of leave requests pending manager approval for a specific manager.
    /// </summary>
    Task<int> GetPendingApprovalCountAsync(Guid managerId, CancellationToken ct = default);
}

/// <summary>
/// Represents a failed LeaveService request with the downstream response preserved.
/// </summary>
public sealed class LeaveServiceRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeaveServiceRequestException"/> class.
    /// </summary>
    public LeaveServiceRequestException(System.Net.HttpStatusCode statusCode, string responseBody)
        : base($"LeaveService request failed with HTTP {(int)statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Gets the downstream HTTP status code.
    /// </summary>
    public System.Net.HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the downstream response body.
    /// </summary>
    public string ResponseBody { get; }

    /// <summary>
    /// Gets the most useful user-facing message from the downstream response.
    /// </summary>
    public string ClientMessage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ResponseBody))
            {
                return "Leave service rejected the request.";
            }

            try
            {
                using var document = JsonDocument.Parse(ResponseBody);
                foreach (var propertyName in new[] { "message", "detail", "title" })
                {
                    if (document.RootElement.TryGetProperty(propertyName, out var property) &&
                        property.ValueKind == JsonValueKind.String)
                    {
                        return property.GetString() ?? ResponseBody;
                    }
                }
            }
            catch (JsonException)
            {
                return ResponseBody;
            }

            return ResponseBody;
        }
    }
}

/// <summary>
/// Default implementation of the leave service client.
/// </summary>
public class LeaveServiceClient(HttpClient httpClient) : ILeaveServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <inheritdoc />
    public async Task<List<LeaveBalanceDto>> GetMyBalancesAsync(Guid employeeId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/leave/v1/LeaveBalances/{employeeId}", ct);
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync(ct);
        return ReadJsonArray(json).Select(MapBalance).ToList();
    }

    /// <inheritdoc />
    public async Task<List<LeaveRequestSummaryDto>> GetMyRequestsAsync(Guid employeeId, int? year, CancellationToken ct = default)
    {
        var url = $"/leave/v1/LeaveRequests/employee/{employeeId}";
        if (year.HasValue) url += $"?year={year.Value}";

        var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return [];
        var json = await response.Content.ReadAsStringAsync(ct);
        return ReadJsonArray(json).Select(MapRequestSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<LeaveRequestDetailDto?> SubmitRequestAsync(Guid employeeId, SubmitLeaveRequestDto request, Guid? approverId = null, CancellationToken ct = default)
    {
        var payload = new
        {
            leave_type = ParseLeaveType(request.LeaveType),
            start_date = ToUtcDate(request.StartDate),
            end_date = ToUtcDate(request.EndDate),
            half_day_period = ParseHalfDayPeriod(request.HalfDayPeriod),
            reason = request.Reason,
            approver_id = approverId
        };

        var response = await httpClient.PostAsJsonAsync($"/leave/v1/LeaveRequests/{employeeId}", payload, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new LeaveServiceRequestException(response.StatusCode, error);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(json);
        var requestId = GetGuid(document.RootElement, "id", "Id");
        if (requestId == Guid.Empty)
        {
            return null;
        }

        return new LeaveRequestDetailDto
        {
            Id = requestId,
            EmployeeId = employeeId,
            LeaveType = NormalizeLeaveType(request.LeaveType),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = CalculateDays(request.StartDate, request.EndDate, request.HalfDayPeriod),
            Reason = request.Reason,
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<List<LeaveRequestDetailDto>> GetPendingApprovalsAsync(Guid managerId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"/leave/v1/LeaveRequests/pending/{managerId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return ReadJsonArray(json).Select(MapRequestDetail).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> ProcessDecisionAsync(Guid requestId, Guid approverId, ApproveRejectLeaveRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            decision = ParseDecision(request.Decision),
            comments = request.Comments
        };

        var response = await httpClient.PostAsJsonAsync($"/leave/v1/LeaveRequests/{requestId}/decision?approverId={approverId}", payload, JsonOptions, ct);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<int> GetPendingApprovalCountAsync(Guid managerId, CancellationToken ct = default)
    {
        var httpResponse = await httpClient.GetAsync($"/leave/v1/LeaveRequests/pending-count?managerId={managerId}", ct);
        if (!httpResponse.IsSuccessStatusCode) return 0;
        var response = await httpResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
        return response.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }

    private static IEnumerable<JsonElement> ReadJsonArray(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(item => item.Clone()).ToList()
            : [];
    }

    private static LeaveBalanceDto MapBalance(JsonElement element) => new()
    {
        LeaveType = FormatLeaveType(GetInt32(element, "leave_type", "leaveType", "LeaveType")),
        Entitlement = GetDecimal(element, "entitled", "Entitled", "entitlement", "Entitlement"),
        Used = GetDecimal(element, "used", "Used"),
        Available = GetDecimal(element, "available", "Available")
    };

    private static LeaveRequestSummaryDto MapRequestSummary(JsonElement element) => new()
    {
        Id = GetGuid(element, "id", "Id"),
        LeaveType = FormatLeaveType(GetInt32(element, "leave_type", "leaveType", "LeaveType")),
        StartDate = GetDateTime(element, "start_date", "startDate", "StartDate"),
        EndDate = GetDateTime(element, "end_date", "endDate", "EndDate"),
        Days = GetDecimal(element, "total_days", "totalDays", "TotalDays", "days", "Days"),
        Status = FormatLeaveStatus(GetStatusValue(element, "status", "Status"))
    };

    private static LeaveRequestDetailDto MapRequestDetail(JsonElement element) => new()
    {
        Id = GetGuid(element, "id", "Id"),
        EmployeeId = GetGuid(element, "employee_id", "employeeId", "EmployeeId"),
        LeaveType = FormatLeaveType(GetInt32(element, "leave_type", "leaveType", "LeaveType")),
        StartDate = GetDateTime(element, "start_date", "startDate", "StartDate"),
        EndDate = GetDateTime(element, "end_date", "endDate", "EndDate"),
        Days = GetDecimal(element, "total_days", "totalDays", "TotalDays", "days", "Days"),
        Reason = GetString(element, "reason", "Reason"),
        Status = FormatLeaveStatus(GetStatusValue(element, "status", "Status")),
        RequestedAt = GetDateTime(element, "created_at", "createdAt", "CreatedAt")
    };

    private static int ParseLeaveType(string? value) =>
        NormalizeLeaveType(value).ToLowerInvariant() switch
        {
            "sick" => 2,
            "personal" => 3,
            "maternity" => 4,
            "paternity" => 5,
            "unpaid" => 6,
            "bereavement" => 7,
            "study" => 8,
            _ => 1
        };

    private static string NormalizeLeaveType(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Annual" : value.Trim();

    private static int ParseHalfDayPeriod(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "morning" => 1,
            "afternoon" => 2,
            _ => 0
        };

    private static int ParseDecision(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "reject" or "rejected" => 3,
            _ => 2
        };

    private static string FormatLeaveType(int value) => value switch
    {
        2 => "Sick",
        3 => "Personal",
        4 => "Maternity",
        5 => "Paternity",
        6 => "Unpaid",
        7 => "Bereavement",
        8 => "Study",
        _ => "Annual"
    };

    private static string FormatLeaveStatus(int value) => value switch
    {
        2 => "Approved",
        3 => "Rejected",
        4 => "Cancelled",
        5 => "PartiallyApproved",
        _ => "Pending"
    };

    private static decimal CalculateDays(DateTime startDate, DateTime endDate, string? halfDayPeriod) =>
        ParseHalfDayPeriod(halfDayPeriod) == 0
            ? Math.Max(1, (decimal)(endDate.Date - startDate.Date).TotalDays + 1)
            : 0.5m;

    private static DateTimeOffset ToUtcDate(DateTime value) =>
        new(DateTime.SpecifyKind(value.Date, DateTimeKind.Utc));

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static Guid GetGuid(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                Guid.TryParse(value.GetString(), out var guid))
            {
                return guid;
            }
        }

        return Guid.Empty;
    }

    private static int GetInt32(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (int.TryParse(text, out number))
                {
                    return number;
                }

                return ParseLeaveType(text);
            }
        }

        return 0;
    }

    private static int GetStatusValue(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return (value.GetString() ?? string.Empty).Trim().ToLowerInvariant() switch
                {
                    "approved" => 2,
                    "rejected" => 3,
                    "cancelled" => 4,
                    "partiallyapproved" or "partially approved" => 5,
                    _ => 1
                };
            }
        }

        return 0;
    }

    private static decimal GetDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.TryGetDecimal(out var number))
            {
                return number;
            }
        }

        return 0;
    }

    private static DateTime GetDateTime(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(value.GetString(), out var dateTime))
            {
                return dateTime.DateTime;
            }
        }

        return DateTime.MinValue;
    }
}

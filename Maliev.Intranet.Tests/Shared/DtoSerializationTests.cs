using System.Text.Json;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Xunit;

namespace Maliev.Intranet.Tests.Shared;

public class DtoSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void LeaveBalanceDto_ShouldRoundtrip()
    {
        var dto = new LeaveBalanceDto { LeaveType = "Annual", Entitlement = 20, Used = 5, Available = 15 };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<LeaveBalanceDto>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(dto.LeaveType, result.LeaveType);
        Assert.Equal(dto.Available, result.Available);
    }

    [Fact]
    public void SubmitLeaveRequestDto_ShouldRoundtrip()
    {
        var dto = new SubmitLeaveRequestDto
        {
            LeaveType = "Sick",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            Reason = "Flu"
        };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<SubmitLeaveRequestDto>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(dto.LeaveType, result.LeaveType);
        Assert.Equal(dto.Reason, result.Reason);
    }

    [Fact]
    public void DeliveryNoteSummaryDto_ShouldRoundtrip()
    {
        var dto = new DeliveryNoteSummaryDto { Id = Guid.NewGuid(), OrderNumber = "ORD-456", Status = "Delivered" };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<DeliveryNoteSummaryDto>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.Status, result.Status);
    }

    [Fact]
    public void ReceiptDto_ShouldRoundtrip()
    {
        var dto = new ReceiptDto { Id = Guid.NewGuid(), ReceiptNumber = "REC-001", TotalAmount = 1000m };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<ReceiptDto>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(dto.ReceiptNumber, result.ReceiptNumber);
        Assert.Equal(dto.TotalAmount, result.TotalAmount);
    }

    [Fact]
    public void CreateDeliveryNoteRequest_ShouldRoundtrip()
    {
        var dto = new CreateDeliveryNoteRequest
        {
            OrderId = Guid.NewGuid(),
            Notes = "Test notes",
            Items = new List<CreateDeliveryNoteItemRequest> { new() { OrderItemId = Guid.NewGuid(), Quantity = 5 } }
        };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<CreateDeliveryNoteRequest>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(dto.OrderId, result.OrderId);
        Assert.Single(result.Items);
    }

    [Fact]
    public void LeaveRequestSummaryDto_ShouldRoundtrip()
    {
        var dto = new LeaveRequestSummaryDto { Id = Guid.NewGuid(), LeaveType = "Annual", Status = "Approved", Days = 3 };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<LeaveRequestSummaryDto>(json, Options);
        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.Status, result.Status);
    }

    [Fact]
    public void CreateReceiptRequest_ShouldRoundtrip()
    {
        var dto = new CreateReceiptRequest { CustomerId = Guid.NewGuid(), Date = DateTime.UtcNow, Lines = [new() { Description = "Test", Amount = 100 }] };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<CreateReceiptRequest>(json, Options);
        Assert.NotNull(result);
        Assert.Equal(dto.CustomerId, result.CustomerId);
        Assert.Single(result.Lines);
    }

    [Fact]
    public void PurchaseOrderDto_ShouldRoundtrip()
    {
        var dto = new PurchaseOrderDto { Id = 123, PoNumber = "PO-123", TotalAmount = 5000m };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<PurchaseOrderDto>(json, Options);
        Assert.NotNull(result);
        Assert.Equal(dto.PoNumber, result.PoNumber);
    }

    [Fact]
    public void PerformanceReviewDto_ShouldRoundtrip()
    {
        var dto = new PerformanceReviewDto { Id = Guid.NewGuid(), Rating = 5, Comments = "Excellent" };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<PerformanceReviewDto>(json, Options);
        Assert.NotNull(result);
        Assert.Equal(dto.Rating, result.Rating);
    }

    [Fact]
    public void OnboardingChecklistDto_ShouldRoundtrip()
    {
        var dto = new OnboardingChecklistDto { Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), Tasks = new List<OnboardingTaskDto> { new() { Title = "Task 1" } } };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<OnboardingChecklistDto>(json, Options);
        Assert.NotNull(result);
        Assert.Single(result.Tasks);
    }

    [Fact]
    public void OnboardingSummaryDto_ShouldRoundtrip()
    {
        var dto = new OnboardingSummaryDto { Id = Guid.NewGuid(), EmployeeName = "John", Progress = 75 };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<OnboardingSummaryDto>(json, Options);
        Assert.NotNull(result);
        Assert.Equal(dto.EmployeeName, result.EmployeeName);
        Assert.Equal(dto.Progress, result.Progress);
    }

    [Fact]
    public void GoalDto_ShouldRoundtrip()
    {
        var dto = new GoalDto { Id = Guid.NewGuid(), Title = "Learn C#", Progress = 50 };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<GoalDto>(json, Options);
        Assert.NotNull(result);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.Progress, result.Progress);
    }
}

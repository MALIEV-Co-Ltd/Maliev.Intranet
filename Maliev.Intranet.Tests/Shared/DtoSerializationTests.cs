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
        var dto = new DeliveryNoteSummaryDto { Id = "DN-2026-000001", DeliveryNoteId = "DN-2026-000001", OrderId = "ORD-456", Status = "Delivered" };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<DeliveryNoteSummaryDto>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.DeliveryNoteId, result.DeliveryNoteId);
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
            OrderId = "ORD-456",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            DeliveryInstructions = "Test notes",
            Items = new List<CreateDeliveryNoteItemRequest>
            {
                new()
                {
                    ProductCode = "PART-1",
                    ProductName = "Test part",
                    QuantityOrdered = 5,
                    QuantityManufactured = 5,
                    QuantityDelivered = 5,
                    UnitOfMeasure = "pcs"
                }
            }
        };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<CreateDeliveryNoteRequest>(json, Options);

        Assert.NotNull(result);
        Assert.Equal(dto.OrderId, result.OrderId);
        Assert.Equal(dto.CustomerId, result.CustomerId);
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
        var dto = new CreateReceiptRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 100m,
            PaymentMethod = "Bank Transfer"
        };
        var json = JsonSerializer.Serialize(dto, Options);
        var result = JsonSerializer.Deserialize<CreateReceiptRequest>(json, Options);
        Assert.NotNull(result);
        Assert.Equal(dto.InvoiceId, result.InvoiceId);
        Assert.Equal(dto.Amount, result.Amount);
        Assert.Equal(dto.PaymentMethod, result.PaymentMethod);
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

    [Fact]
    public void BffMigrateProjectResponseDto_ShouldUseCamelCaseWireShape_WithNestedStatus()
    {
        var dto = new BffMigrateProjectResponseDto
        {
            DryRun = false,
            TotalEvaluated = 1,
            TotalMigrated = 1,
            MigratedFiles =
            [
                new BffMigratedProjectFileDto
                {
                    FileId = "file-1",
                    OldPath = "projects/project-1/part.stl",
                    NewPath = "customers/customer-1/projects/project-1/part.stl",
                    Status = new FileAnalysisStatusDto
                    {
                        UploadId = "customers/customer-1/projects/project-1/part.stl",
                        Status = FileAnalysisStatus.Completed,
                        Dimensions = new FileAnalysisDimensionsDto
                        {
                            X = 10,
                            Y = 20,
                            Z = 30,
                            VolumeMm3 = 6000
                        },
                        PreviewProcessingStatus = PreviewProcessingStatus.Completed
                    }
                }
            ],
            Errors = []
        };
        var webOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var json = JsonSerializer.Serialize(dto, webOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("migratedFiles", out var migratedFiles));
        Assert.False(root.TryGetProperty("migrated_files", out _));
        var file = migratedFiles.EnumerateArray().Single();
        Assert.True(file.TryGetProperty("fileId", out _));
        Assert.True(file.TryGetProperty("oldPath", out _));
        Assert.True(file.TryGetProperty("newPath", out _));
        Assert.True(file.TryGetProperty("status", out var status));
        Assert.Equal((int)FileAnalysisStatus.Completed, status.GetProperty("status").GetInt32());

        var result = JsonSerializer.Deserialize<BffMigrateProjectResponseDto>(json, webOptions);

        Assert.NotNull(result);
        var migrated = Assert.Single(result.MigratedFiles);
        Assert.Equal("file-1", migrated.FileId);
        Assert.Equal(FileAnalysisStatus.Completed, migrated.Status?.Status);
        Assert.Equal(6000, migrated.Status?.Dimensions?.VolumeMm3);
    }
}

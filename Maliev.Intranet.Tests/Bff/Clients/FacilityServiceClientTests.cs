using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class FacilityServiceClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly FacilityServiceClient _client;

    public FacilityServiceClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://test")
        };
        _client = new FacilityServiceClient(httpClient);
    }

    // -----------------------------------------------------------------------
    // Equipment
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetEquipmentsAsync_ShouldReturnPagedResult()
    {
        var pagedResult = new
        {
            items = new List<EquipmentSummaryDto> { new() { Name = "Bambu P1S" } },
            totalCount = 1,
            page = 1,
            pageSize = 20
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains("/facility/v1/equipments")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(pagedResult)
            });

        var result = await _client.GetEquipmentsAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetEquipmentByIdAsync_ShouldReturnDetail()
    {
        var id = Guid.NewGuid();
        var detail = new EquipmentDetailDto { Id = id, Name = "HAAS VF-2" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(detail)
            });

        var result = await _client.GetEquipmentByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("HAAS VF-2", result.Name);
    }

    [Fact]
    public async Task RegisterEquipmentAsync_ShouldReturnCreatedDetail()
    {
        var request = new RegisterEquipmentRequest { Name = "Bambu P1S", Category = "FdmPrinter" };
        var created = new EquipmentDetailDto { Id = Guid.NewGuid(), Name = "Bambu P1S" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Post &&
                    m.RequestUri!.PathAndQuery.Contains("/facility/v1/equipments")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(created)
            });

        var result = await _client.RegisterEquipmentAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Bambu P1S", result.Name);
    }

    [Fact]
    public async Task RegisterEquipmentAsync_ShouldReturnNull_WhenFails()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity));

        var result = await _client.RegisterEquipmentAsync(new RegisterEquipmentRequest());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateEquipmentAsync_ShouldReturnUpdatedDetail()
    {
        var id = Guid.NewGuid();
        var updated = new EquipmentDetailDto { Id = id, Name = "HAAS VF-2 Updated" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Put &&
                    m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(updated)
            });

        var result = await _client.UpdateEquipmentAsync(id, new UpdateEquipmentRequest());

        Assert.NotNull(result);
        Assert.Equal("HAAS VF-2 Updated", result.Name);
    }

    [Fact]
    public async Task ChangeEquipmentStatusAsync_ShouldReturnUpdatedDetail()
    {
        var id = Guid.NewGuid();
        var updated = new EquipmentDetailDto { Id = id, Status = "UnderMaintenance" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}/status")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(updated)
            });

        var result = await _client.ChangeEquipmentStatusAsync(id, new ChangeEquipmentStatusRequest { NewStatus = "UnderMaintenance" });

        Assert.NotNull(result);
        Assert.Equal("UnderMaintenance", result.Status);
    }

    [Fact]
    public async Task DeleteEquipmentAsync_ShouldReturnTrue_WhenSuccess()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        var result = await _client.DeleteEquipmentAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteEquipmentAsync_ShouldReturnFalse_WhenConflict()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

        var result = await _client.DeleteEquipmentAsync(Guid.NewGuid());

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // Notes
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetNotesAsync_ShouldReturnNotes()
    {
        var id = Guid.NewGuid();
        var notes = new List<EquipmentNoteDto> { new() { Content = "Checked nozzle." } };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}/notes")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(notes)
            });

        var result = await _client.GetNotesAsync(id);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task AddNoteAsync_ShouldReturnCreatedNote()
    {
        var id = Guid.NewGuid();
        var note = new EquipmentNoteDto { Content = "Replaced PTFE tube." };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Post &&
                    m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}/notes")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(note)
            });

        var result = await _client.AddNoteAsync(id, new AddEquipmentNoteRequest { Content = "Replaced PTFE tube." });

        Assert.NotNull(result);
        Assert.Equal("Replaced PTFE tube.", result.Content);
    }

    // -----------------------------------------------------------------------
    // Loans
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLoansAsync_ShouldReturnLoans()
    {
        var id = Guid.NewGuid();
        var loans = new List<EquipmentLoanDto> { new() { LoanStatus = "Active" } };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}/loans")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(loans)
            });

        var result = await _client.GetLoansAsync(id);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task CreateLoanAsync_ShouldReturnCreatedLoan()
    {
        var loan = new EquipmentLoanDto { LoanStatus = "Pending" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Post &&
                    m.RequestUri!.PathAndQuery.Contains("/facility/v1/loans")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(loan)
            });

        var result = await _client.CreateLoanAsync(new CreateLoanRequest
        {
            EquipmentId = Guid.NewGuid(),
            BorrowerId = Guid.NewGuid(),
            BorrowerType = "Employee",
            Purpose = "Workshop use",
            LoanStartDate = DateOnly.FromDateTime(DateTime.Today),
            ExpectedReturnDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        });

        Assert.NotNull(result);
        Assert.Equal("Pending", result.LoanStatus);
    }

    [Fact]
    public async Task ApproveLoanAsync_ShouldReturnApprovedLoan()
    {
        var loanId = Guid.NewGuid();
        var loan = new EquipmentLoanDto { LoanStatus = "Active" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/loans/{loanId}/approve")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(loan)
            });

        var result = await _client.ApproveLoanAsync(loanId, new ApproveLoanRequest { BorrowerDisplayName = "John" });

        Assert.NotNull(result);
        Assert.Equal("Active", result.LoanStatus);
    }

    [Fact]
    public async Task RejectLoanAsync_ShouldReturnRejectedLoan()
    {
        var loanId = Guid.NewGuid();
        var loan = new EquipmentLoanDto { LoanStatus = "Rejected" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/loans/{loanId}/reject")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(loan)
            });

        var result = await _client.RejectLoanAsync(loanId, new RejectLoanRequest());

        Assert.NotNull(result);
        Assert.Equal("Rejected", result.LoanStatus);
    }

    [Fact]
    public async Task ReturnLoanAsync_ShouldReturnReturnedLoan()
    {
        var loanId = Guid.NewGuid();
        var loan = new EquipmentLoanDto { LoanStatus = "Returned" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/loans/{loanId}/return")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(loan)
            });

        var result = await _client.ReturnLoanAsync(loanId, new ReturnLoanRequest
        {
            ActualReturnDate = DateOnly.FromDateTime(DateTime.Today)
        });

        Assert.NotNull(result);
        Assert.Equal("Returned", result.LoanStatus);
    }

    // -----------------------------------------------------------------------
    // Maintenance Logs
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMaintenanceLogsAsync_ShouldReturnLogs()
    {
        var id = Guid.NewGuid();
        var logs = new List<MaintenanceLogDto> { new() { Type = "Preventive", Description = "Lubricated rails." } };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}/maintenance")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(logs)
            });

        var result = await _client.GetMaintenanceLogsAsync(id);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Preventive", result[0].Type);
    }

    [Fact]
    public async Task AddMaintenanceLogAsync_ShouldReturnCreatedLog()
    {
        var id = Guid.NewGuid();
        var log = new MaintenanceLogDto { Type = "Calibration", Description = "Bed levelled." };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Post &&
                    m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}/maintenance")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(log)
            });

        var result = await _client.AddMaintenanceLogAsync(id, new AddMaintenanceLogRequest
        {
            Type = "Calibration",
            Description = "Bed levelled.",
            OccurredAt = DateTime.Today
        });

        Assert.NotNull(result);
        Assert.Equal("Calibration", result.Type);
    }

    // -----------------------------------------------------------------------
    // CNC Attachments
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAttachmentsAsync_ShouldReturnAttachments()
    {
        var id = Guid.NewGuid();
        var attachments = new List<EquipmentAttachmentDto> { new() { Name = "4th Axis", AttachmentType = "RotaryAxis" } };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}/attachments")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(attachments)
            });

        var result = await _client.GetAttachmentsAsync(id);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("4th Axis", result[0].Name);
    }

    [Fact]
    public async Task AddAttachmentAsync_ShouldReturnCreatedAttachment()
    {
        var id = Guid.NewGuid();
        var attachment = new EquipmentAttachmentDto { Name = "Probing Arm", AttachmentType = "Probe" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Post &&
                    m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{id}/attachments")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(attachment)
            });

        var result = await _client.AddAttachmentAsync(id, new AddAttachmentRequest { Name = "Probing Arm", AttachmentType = "Probe" });

        Assert.NotNull(result);
        Assert.Equal("Probing Arm", result.Name);
    }

    [Fact]
    public async Task UpdateAttachmentAsync_ShouldReturnUpdatedAttachment()
    {
        var equipmentId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var updated = new EquipmentAttachmentDto { Name = "Probing Arm v2", IsActive = false };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Put &&
                    m.RequestUri!.PathAndQuery.Contains($"/facility/v1/equipments/{equipmentId}/attachments/{attachmentId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(updated)
            });

        var result = await _client.UpdateAttachmentAsync(equipmentId, attachmentId, new UpdateAttachmentRequest());

        Assert.NotNull(result);
        Assert.Equal("Probing Arm v2", result.Name);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateAttachmentAsync_ShouldReturnNull_WhenNotFound()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _client.UpdateAttachmentAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateAttachmentRequest());

        Assert.Null(result);
    }
}

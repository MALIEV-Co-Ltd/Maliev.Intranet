using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class EquipmentsControllerTests
{
    private readonly Mock<FacilityServiceClient> _clientMock;
    private readonly EquipmentsController _controller;

    public EquipmentsControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _clientMock = new Mock<FacilityServiceClient>(httpClient);
        _controller = new EquipmentsController(_clientMock.Object);
    }

    // -----------------------------------------------------------------------
    // Get (list)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Get_ShouldReturnOkWithPagedResponse_WhenClientReturnsData()
    {
        var facilityResult = new FacilityPagedResult<EquipmentSummaryDto>
        {
            Items = [new EquipmentSummaryDto { Name = "Bambu P1S" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _clientMock.Setup(x => x.GetEquipmentsAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facilityResult);

        var result = await _controller.Get(null, null, null, 1, 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResponse<EquipmentSummaryDto>>(ok.Value);
        Assert.Single(paged.Data);
        Assert.Equal(1, paged.Meta.TotalCount);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyPagedResponse_WhenClientReturnsNull()
    {
        _clientMock.Setup(x => x.GetEquipmentsAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FacilityPagedResult<EquipmentSummaryDto>?)null);

        var result = await _controller.Get(null, null, null, 1, 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResponse<EquipmentSummaryDto>>(ok.Value);
        Assert.Empty(paged.Data);
    }

    // -----------------------------------------------------------------------
    // GetById
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenEquipmentExists()
    {
        var id = Guid.NewGuid();
        var detail = new EquipmentDetailDto { Id = id, Name = "HAAS VF-2" };
        _clientMock.Setup(x => x.GetEquipmentByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(detail, ok.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenEquipmentMissing()
    {
        _clientMock.Setup(x => x.GetEquipmentByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentDetailDto?)null);

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -----------------------------------------------------------------------
    // Register
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Register_ShouldReturnCreatedAtAction_WhenSuccessful()
    {
        var request = new RegisterEquipmentRequest { Name = "Bambu P1S", Category = "FdmPrinter" };
        var created = new EquipmentDetailDto { Id = Guid.NewGuid(), Name = "Bambu P1S" };
        _clientMock.Setup(x => x.RegisterEquipmentAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await _controller.Register(request, CancellationToken.None);

        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(created, createdAt.Value);
        Assert.Equal(nameof(_controller.GetById), createdAt.ActionName);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenClientFails()
    {
        _clientMock.Setup(x => x.RegisterEquipmentAsync(It.IsAny<RegisterEquipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentDetailDto?)null);

        var result = await _controller.Register(new RegisterEquipmentRequest(), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Update_ShouldReturnOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        var updated = new EquipmentDetailDto { Id = id, Name = "Updated" };
        _clientMock.Setup(x => x.UpdateEquipmentAsync(id, It.IsAny<UpdateEquipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _controller.Update(id, new UpdateEquipmentRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenEquipmentMissing()
    {
        _clientMock.Setup(x => x.UpdateEquipmentAsync(It.IsAny<Guid>(), It.IsAny<UpdateEquipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentDetailDto?)null);

        var result = await _controller.Update(Guid.NewGuid(), new UpdateEquipmentRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -----------------------------------------------------------------------
    // ChangeStatus
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ChangeStatus_ShouldReturnOk_WhenTransitionValid()
    {
        var id = Guid.NewGuid();
        var updated = new EquipmentDetailDto { Id = id, Status = "UnderMaintenance" };
        _clientMock.Setup(x => x.ChangeEquipmentStatusAsync(id, It.IsAny<ChangeEquipmentStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _controller.ChangeStatus(id, new ChangeEquipmentStatusRequest { NewStatus = "UnderMaintenance" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReturnNotFound_WhenEquipmentMissing()
    {
        _clientMock.Setup(x => x.ChangeEquipmentStatusAsync(It.IsAny<Guid>(), It.IsAny<ChangeEquipmentStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentDetailDto?)null);

        var result = await _controller.ChangeStatus(Guid.NewGuid(), new ChangeEquipmentStatusRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // -----------------------------------------------------------------------
    // Delete
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenSuccessful()
    {
        _clientMock.Setup(x => x.DeleteEquipmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenFails()
    {
        _clientMock.Setup(x => x.DeleteEquipmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // -----------------------------------------------------------------------
    // Notes
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetNotes_ShouldReturnOkWithNotes()
    {
        var id = Guid.NewGuid();
        IReadOnlyList<EquipmentNoteDto> notes = [new() { Content = "Checked nozzle." }];
        _clientMock.Setup(x => x.GetNotesAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        var result = await _controller.GetNotes(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(notes, ok.Value);
    }

    [Fact]
    public async Task GetNotes_ShouldReturnEmptyList_WhenClientReturnsNull()
    {
        _clientMock.Setup(x => x.GetNotesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EquipmentNoteDto>?)null);

        var result = await _controller.GetNotes(Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<EquipmentNoteDto>>(ok.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task AddNote_ShouldReturnOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        var request = new AddEquipmentNoteRequest { Content = "Replaced PTFE tube." };
        var note = new EquipmentNoteDto { Content = "Replaced PTFE tube." };
        _clientMock.Setup(x => x.AddNoteAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        var result = await _controller.AddNote(id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(note, ok.Value);
    }

    [Fact]
    public async Task AddNote_ShouldReturnBadRequest_WhenClientFails()
    {
        _clientMock.Setup(x => x.AddNoteAsync(It.IsAny<Guid>(), It.IsAny<AddEquipmentNoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentNoteDto?)null);

        var result = await _controller.AddNote(Guid.NewGuid(), new AddEquipmentNoteRequest(), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    // -----------------------------------------------------------------------
    // Loans
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLoans_ShouldReturnOkWithLoans()
    {
        var id = Guid.NewGuid();
        IReadOnlyList<EquipmentLoanDto> loans = [new() { LoanStatus = "Active" }];
        _clientMock.Setup(x => x.GetLoansAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loans);

        var result = await _controller.GetLoans(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(loans, ok.Value);
    }

    [Fact]
    public async Task CreateLoan_ShouldReturnOk_WhenSuccessful()
    {
        var loan = new EquipmentLoanDto { LoanStatus = "Pending" };
        _clientMock.Setup(x => x.CreateLoanAsync(It.IsAny<CreateLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await _controller.CreateLoan(new CreateLoanRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(loan, ok.Value);
    }

    [Fact]
    public async Task CreateLoan_ShouldReturnBadRequest_WhenClientFails()
    {
        _clientMock.Setup(x => x.CreateLoanAsync(It.IsAny<CreateLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentLoanDto?)null);

        var result = await _controller.CreateLoan(new CreateLoanRequest(), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task ApproveLoan_ShouldReturnOk_WhenSuccessful()
    {
        var loanId = Guid.NewGuid();
        var loan = new EquipmentLoanDto { LoanStatus = "Active" };
        _clientMock.Setup(x => x.ApproveLoanAsync(loanId, It.IsAny<ApproveLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await _controller.ApproveLoan(loanId, new ApproveLoanRequest { BorrowerDisplayName = "John" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(loan, ok.Value);
    }

    [Fact]
    public async Task ApproveLoan_ShouldReturnNotFound_WhenLoanMissing()
    {
        _clientMock.Setup(x => x.ApproveLoanAsync(It.IsAny<Guid>(), It.IsAny<ApproveLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentLoanDto?)null);

        var result = await _controller.ApproveLoan(Guid.NewGuid(), new ApproveLoanRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task RejectLoan_ShouldReturnOk_WhenSuccessful()
    {
        var loanId = Guid.NewGuid();
        var loan = new EquipmentLoanDto { LoanStatus = "Rejected" };
        _clientMock.Setup(x => x.RejectLoanAsync(loanId, It.IsAny<RejectLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await _controller.RejectLoan(loanId, new RejectLoanRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(loan, ok.Value);
    }

    [Fact]
    public async Task ReturnLoan_ShouldReturnOk_WhenSuccessful()
    {
        var loanId = Guid.NewGuid();
        var loan = new EquipmentLoanDto { LoanStatus = "Returned" };
        _clientMock.Setup(x => x.ReturnLoanAsync(loanId, It.IsAny<ReturnLoanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await _controller.ReturnLoan(loanId, new ReturnLoanRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(loan, ok.Value);
    }

    // -----------------------------------------------------------------------
    // Maintenance Logs
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMaintenanceLogs_ShouldReturnOkWithLogs()
    {
        var id = Guid.NewGuid();
        IReadOnlyList<MaintenanceLogDto> logs = [new() { Type = "Preventive" }];
        _clientMock.Setup(x => x.GetMaintenanceLogsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var result = await _controller.GetMaintenanceLogs(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(logs, ok.Value);
    }

    [Fact]
    public async Task AddMaintenanceLog_ShouldReturnOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        var request = new AddMaintenanceLogRequest { Type = "Calibration", Description = "Bed levelled.", OccurredAt = DateOnly.FromDateTime(DateTime.Today) };
        var log = new MaintenanceLogDto { Type = "Calibration" };
        _clientMock.Setup(x => x.AddMaintenanceLogAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        var result = await _controller.AddMaintenanceLog(id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(log, ok.Value);
    }

    [Fact]
    public async Task AddMaintenanceLog_ShouldReturnBadRequest_WhenClientFails()
    {
        _clientMock.Setup(x => x.AddMaintenanceLogAsync(It.IsAny<Guid>(), It.IsAny<AddMaintenanceLogRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaintenanceLogDto?)null);

        var result = await _controller.AddMaintenanceLog(Guid.NewGuid(), new AddMaintenanceLogRequest(), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    // -----------------------------------------------------------------------
    // CNC Attachments
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAttachments_ShouldReturnOkWithAttachments()
    {
        var id = Guid.NewGuid();
        IReadOnlyList<EquipmentAttachmentDto> attachments = [new() { Name = "4th Axis" }];
        _clientMock.Setup(x => x.GetAttachmentsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachments);

        var result = await _controller.GetAttachments(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(attachments, ok.Value);
    }

    [Fact]
    public async Task AddAttachment_ShouldReturnOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        var attachment = new EquipmentAttachmentDto { Name = "Probing Arm" };
        _clientMock.Setup(x => x.AddAttachmentAsync(id, It.IsAny<AddAttachmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);

        var result = await _controller.AddAttachment(id, new AddAttachmentRequest { Name = "Probing Arm", AttachmentType = "Probe" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(attachment, ok.Value);
    }

    [Fact]
    public async Task AddAttachment_ShouldReturnBadRequest_WhenClientFails()
    {
        _clientMock.Setup(x => x.AddAttachmentAsync(It.IsAny<Guid>(), It.IsAny<AddAttachmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentAttachmentDto?)null);

        var result = await _controller.AddAttachment(Guid.NewGuid(), new AddAttachmentRequest(), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAttachment_ShouldReturnOk_WhenSuccessful()
    {
        var id = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var updated = new EquipmentAttachmentDto { Name = "Updated Arm" };
        _clientMock.Setup(x => x.UpdateAttachmentAsync(id, attachmentId, It.IsAny<UpdateAttachmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _controller.UpdateAttachment(id, attachmentId, new UpdateAttachmentRequest(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task UpdateAttachment_ShouldReturnNotFound_WhenMissing()
    {
        _clientMock.Setup(x => x.UpdateAttachmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateAttachmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EquipmentAttachmentDto?)null);

        var result = await _controller.UpdateAttachment(Guid.NewGuid(), Guid.NewGuid(), new UpdateAttachmentRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}

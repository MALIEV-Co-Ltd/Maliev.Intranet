using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Unit tests for <see cref="ProjectsController"/> using a mock HTTP client.
/// </summary>
public class ProjectsControllerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly ILogger<ProjectsController> Logger =
        NullLogger<ProjectsController>.Instance;

    private static ProjectServiceClient CreateClient<T>(T responseBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = JsonContent.Create(responseBody) }));
        return new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static ProjectServiceClient CreateRawClient(HttpStatusCode status)
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(status)));
        return new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static JobServiceClient StubJobClient()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        return new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static FacilityServiceClient StubFacilityClient()
    {
        var handler = new MockHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        return new FacilityServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    // ── GET (list) ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ShouldReturnOk_WithPagedResponse()
    {
        var paged = new PagedResponse<ProjectSummaryDto> { Data = new List<ProjectSummaryDto>() };
        var controller = new ProjectsController(CreateClient(paged), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Get(ct: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PagedResponse<ProjectSummaryDto>>(ok.Value);
    }

    [Fact]
    public async Task Get_WithStatusFilter_ShouldPassQueryToClient()
    {
        // Verify the query string is forwarded (handler logs requests)
        string? capturedUrl = null;
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedUrl = req.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PagedResponse<ProjectSummaryDto>())
            });
        });
        var client = new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new ProjectsController(client, StubJobClient(), StubFacilityClient(), Logger);

        await controller.Get(status: "Configuring", ct: CancellationToken.None);

        Assert.NotNull(capturedUrl);
        Assert.Contains("status=Configuring", capturedUrl);
    }

    // ── GET by ID ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOk()
    {
        var project = new ProjectDetailDto { Id = Guid.NewGuid(), ProjectNumber = "PRJ-001" };
        var controller = new ProjectsController(CreateClient(project), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetById(project.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NotFound), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturn201Created()
    {
        var created = new ProjectDetailDto { Id = Guid.NewGuid(), ProjectNumber = "PRJ-002" };
        var controller = new ProjectsController(CreateClient(created, HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Create(new CreateProjectRequest
        {
            CustomerId = Guid.NewGuid(),
            Title = "Test Project"
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenDownstreamFails_ShouldReturn502()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.InternalServerError), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Create(new CreateProjectRequest
        {
            CustomerId = Guid.NewGuid(),
            Title = "Test"
        }, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, status.StatusCode);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Update(Guid.NewGuid(), new { title = "Updated" }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_WhenDownstreamFails_ShouldReturnErrorStatusCode()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.Conflict), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Update(Guid.NewGuid(), new { title = "Conflict" }, CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(409, status.StatusCode);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NoContent), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Parts ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddPart_WhenSuccessful_ShouldReturnOk()
    {
        var part = new ProjectPartDto { Id = Guid.NewGuid(), FileName = "bracket.stl" };
        var controller = new ProjectsController(CreateClient(part), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.AddPart(Guid.NewGuid(),
            new AddProjectPartRequest { FileId = Guid.NewGuid(), FileName = "bracket.stl" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task AddPart_WhenProjectServiceReturnsBadRequest_ShouldForwardBadRequest()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.BadRequest), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.AddPart(Guid.NewGuid(),
            new AddProjectPartRequest { FileId = Guid.NewGuid(), FileName = "bracket.stl" },
            CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, status.StatusCode);
    }

    [Fact]
    public async Task UpdatePart_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.UpdatePart(Guid.NewGuid(), Guid.NewGuid(),
            new UpdateProjectPartRequest { ProcessType = "FDM", Quantity = 2 },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePart_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NoContent), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.DeletePart(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Pricing ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPartPrice_WhenSuccessful_ShouldReturnBreakdown()
    {
        var projectServicePart = new { effectiveUnitPrice = 250m };
        var controller = new ProjectsController(CreateClient(projectServicePart), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartPrice(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProjectPriceBreakdownDto>(ok.Value);
        Assert.Equal(250m, dto.TotalPerUnit);
    }

    [Fact]
    public async Task GetPartPrice_WhenDownstreamFails_ShouldReturn502()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.ServiceUnavailable), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartPrice(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(502, status.StatusCode);
    }

    [Fact]
    public async Task ConfirmPartPrice_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.ConfirmPartPrice(
            Guid.NewGuid(), Guid.NewGuid(),
            new ConfirmPartPriceRequest { ConfirmedUnitPrice = 300m },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Quotation lifecycle ───────────────────────────────────────────────────

    [Fact]
    public async Task GenerateQuotation_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GenerateQuotation(Guid.NewGuid(), new GenerateQuotationRequest(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GenerateQuotation_WhenDownstreamFails_ShouldReturnErrorStatusCode()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.UnprocessableEntity), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GenerateQuotation(Guid.NewGuid(), new GenerateQuotationRequest(), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(422, status.StatusCode);
    }

    [Fact]
    public async Task AcceptQuotation_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.AcceptQuotation(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Routing ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPartRouting_MissingProcessType_ReturnsBadRequest()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPartRouting_EmptyProcessType_ReturnsBadRequest()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPartRouting_BothServicesSucceed_ReturnsRoutingWithQueueAndMachine()
    {
        var queueJson = "{\"FDM\":3}";
        var machineJson = "{\"Items\":[{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"AssetCode\":\"MAL-FDM-001\",\"Name\":\"Bambu X1C\",\"Category\":\"FdmPrinter\",\"Status\":\"Active\",\"UpdatedAt\":\"2026-01-01T00:00:00Z\"}],\"TotalCount\":1,\"Page\":1}";

        var jobHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(queueJson, System.Text.Encoding.UTF8, "application/json") }));
        var jobClient = new JobServiceClient(new HttpClient(jobHandler) { BaseAddress = new Uri("http://test") });

        var facilityHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(machineJson, System.Text.Encoding.UTF8, "application/json") }));
        var facilityClient = new FacilityServiceClient(new HttpClient(facilityHandler) { BaseAddress = new Uri("http://test") });

        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), jobClient, facilityClient, Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "FDM", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal(3, dto.QueueAhead);
        Assert.Equal("MAL-FDM-001", dto.MachineCode);
        Assert.Equal("Bambu X1C", dto.MachineName);
        Assert.Equal(new Guid("11111111-1111-1111-1111-111111111111"), dto.MachineId);
        // Non-CNC: setupDays=1 + queueAhead=3 = 4 days minimum
        Assert.True(dto.EstimatedStartDate >= DateTimeOffset.UtcNow.AddDays(3));
    }

    [Fact]
    public async Task GetPartRouting_JobServiceDown_ReturnsZeroQueueWithMachine()
    {
        var machineJson = "{\"Items\":[{\"Id\":\"22222222-2222-2222-2222-222222222222\",\"AssetCode\":\"MAL-FDM-002\",\"Name\":\"Bambu P1S\",\"Category\":\"FdmPrinter\",\"Status\":\"Active\",\"UpdatedAt\":\"2026-01-01T00:00:00Z\"}],\"TotalCount\":1,\"Page\":1}";

        var facilityHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(machineJson, System.Text.Encoding.UTF8, "application/json") }));
        var facilityClient = new FacilityServiceClient(new HttpClient(facilityHandler) { BaseAddress = new Uri("http://test") });

        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), facilityClient, Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "FDM", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal(0, dto.QueueAhead);
        Assert.Equal("MAL-FDM-002", dto.MachineCode);
    }

    [Fact]
    public async Task GetPartRouting_FacilityServiceDown_ReturnsFallbackMachineFields()
    {
        var queueJson = "{\"FDM\":1}";
        var jobHandler = new MockHttpMessageHandler((req, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent(queueJson, System.Text.Encoding.UTF8, "application/json") }));
        var jobClient = new JobServiceClient(new HttpClient(jobHandler) { BaseAddress = new Uri("http://test") });

        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), jobClient, StubFacilityClient(), Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "FDM", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal(1, dto.QueueAhead);
        Assert.Equal("TBD", dto.MachineCode);
        Assert.Equal("Unassigned", dto.MachineName);
        Assert.Equal(Guid.Empty, dto.MachineId);
    }

    [Fact]
    public async Task GetPartRouting_CncProcess_UsesTwoSetupDays()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);
        var before = DateTimeOffset.UtcNow.AddDays(2); // setup=2, queue=0

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "CNC_MILL", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.True(dto.EstimatedStartDate >= before);
    }

    [Fact]
    public async Task GetPartRouting_UnknownProcessType_ReturnsOkWithFallbackMachineFields()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK), StubJobClient(), StubFacilityClient(), Logger);

        var result = await controller.GetPartRouting(Guid.NewGuid(), Guid.NewGuid(), processType: "UNKNOWN_PROCESS", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductionRoutingDto>(ok.Value);
        Assert.Equal("TBD", dto.MachineCode);
        Assert.Equal("Unassigned", dto.MachineName);
    }
}

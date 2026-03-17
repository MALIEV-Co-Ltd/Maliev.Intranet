using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Unit tests for <see cref="ProjectsController"/> using a mock HTTP client.
/// </summary>
public class ProjectsControllerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

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

    // ── GET (list) ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ShouldReturnOk_WithPagedResponse()
    {
        var paged = new PagedResponse<ProjectSummaryDto> { Data = new List<ProjectSummaryDto>() };
        var controller = new ProjectsController(CreateClient(paged));

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
        var client     = new ProjectServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new ProjectsController(client);

        await controller.Get(status: "Configuring", ct: CancellationToken.None);

        Assert.NotNull(capturedUrl);
        Assert.Contains("status=Configuring", capturedUrl);
    }

    // ── GET by ID ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenFound_ShouldReturnOk()
    {
        var project = new ProjectDetailDto { Id = Guid.NewGuid(), ProjectNumber = "PRJ-001" };
        var controller = new ProjectsController(CreateClient(project));

        var result = await controller.GetById(project.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NotFound));

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenSuccessful_ShouldReturn201Created()
    {
        var created = new ProjectDetailDto { Id = Guid.NewGuid(), ProjectNumber = "PRJ-002" };
        var controller = new ProjectsController(CreateClient(created, HttpStatusCode.OK));

        var result = await controller.Create(new CreateProjectRequest
        {
            CustomerId = Guid.NewGuid(),
            Title      = "Test Project"
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenDownstreamFails_ShouldReturn502()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.InternalServerError));

        var result = await controller.Create(new CreateProjectRequest
        {
            CustomerId = Guid.NewGuid(),
            Title      = "Test"
        }, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(502, status.StatusCode);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.Update(Guid.NewGuid(), new { title = "Updated" }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_WhenDownstreamFails_ShouldReturnErrorStatusCode()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.Conflict));

        var result = await controller.Update(Guid.NewGuid(), new { title = "Conflict" }, CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(409, status.StatusCode);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NoContent));

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Parts ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddPart_WhenSuccessful_ShouldReturnOk()
    {
        var part       = new ProjectPartDto { Id = Guid.NewGuid(), FileName = "bracket.stl" };
        var controller = new ProjectsController(CreateClient(part));

        var result = await controller.AddPart(Guid.NewGuid(),
            new AddProjectPartRequest { FileId = Guid.NewGuid(), FileName = "bracket.stl" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task UpdatePart_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.UpdatePart(Guid.NewGuid(), Guid.NewGuid(),
            new UpdateProjectPartRequest { ProcessType = "FDM", Quantity = 2 },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePart_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.NoContent));

        var result = await controller.DeletePart(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Pricing ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPartPrice_WhenSuccessful_ShouldReturnBreakdown()
    {
        var breakdown  = new ProjectPriceBreakdownDto { TotalPerUnit = 250m };
        var controller = new ProjectsController(CreateClient(breakdown));

        var result = await controller.GetPartPrice(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProjectPriceBreakdownDto>(ok.Value);
        Assert.Equal(250m, dto.TotalPerUnit);
    }

    [Fact]
    public async Task GetPartPrice_WhenDownstreamFails_ShouldReturn502()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.ServiceUnavailable));

        var result = await controller.GetPartPrice(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(502, status.StatusCode);
    }

    [Fact]
    public async Task ConfirmPartPrice_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.ConfirmPartPrice(
            Guid.NewGuid(), Guid.NewGuid(),
            new ConfirmPartPriceRequest { ConfirmedPrice = 300m },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    // ── Quotation lifecycle ───────────────────────────────────────────────────

    [Fact]
    public async Task GenerateQuotation_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.GenerateQuotation(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GenerateQuotation_WhenDownstreamFails_ShouldReturnErrorStatusCode()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.UnprocessableEntity));

        var result = await controller.GenerateQuotation(Guid.NewGuid(), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(422, status.StatusCode);
    }

    [Fact]
    public async Task AcceptQuotation_WhenSuccessful_ShouldReturnNoContent()
    {
        var controller = new ProjectsController(CreateRawClient(HttpStatusCode.OK));

        var result = await controller.AcceptQuotation(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}

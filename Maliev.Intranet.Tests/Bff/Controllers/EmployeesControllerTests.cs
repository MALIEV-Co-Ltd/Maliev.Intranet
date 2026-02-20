using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class EmployeesControllerTests
{
    private readonly EmployeeServiceClient _client;
    private readonly EmployeesController _controller;
    private HttpResponseMessage _nextResponse = new(HttpStatusCode.OK);

    public EmployeesControllerTests()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(_nextResponse));
        _client = new EmployeeServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        _controller = new EmployeesController(_client);
    }

    private void SetResponse(HttpResponseMessage response) => _nextResponse = response;
    private void SetJsonResponse<T>(T content, HttpStatusCode status = HttpStatusCode.OK)
        => _nextResponse = new HttpResponseMessage(status) { Content = JsonContent.Create(content) };

    [Fact]
    public async Task Get_ReturnsOk()
    {
        SetJsonResponse(new PagedResponse<EmployeeSummaryDto>());
        var result = await _controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        SetJsonResponse(new EmployeeDetailDto());
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        SetResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenSucceeds_ReturnsCreated()
    {
        var id = Guid.NewGuid();
        SetJsonResponse(new EmployeeDetailDto { Id = id });
        var result = await _controller.Create(new CreateEmployeeRequest(), CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Update_WhenSucceeds_ReturnsOk()
    {
        SetJsonResponse(new EmployeeDetailDto());
        var result = await _controller.Update(Guid.NewGuid(), new UpdateEmployeeRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Terminate_WhenSucceeds_ReturnsOk()
    {
        SetResponse(new HttpResponseMessage(HttpStatusCode.OK));
        var result = await _controller.Terminate(Guid.NewGuid(), new TerminateEmployeeRequest(), CancellationToken.None);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetAnalytics_ReturnsOk()
    {
        SetJsonResponse(new HrAnalyticsDto());
        var result = await _controller.GetAnalytics(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetOrgChart_ReturnsOk()
    {
        SetJsonResponse(new List<OrgNodeDto>());
        var result = await _controller.GetOrgChart(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddNote_WhenSucceeds_ReturnsCreated()
    {
        SetResponse(new HttpResponseMessage(HttpStatusCode.Created));
        var result = await _controller.AddNote(Guid.NewGuid(), new AddEmployeeNoteRequest { Content = "Note" }, CancellationToken.None);
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(201, statusCodeResult.StatusCode);
    }
}

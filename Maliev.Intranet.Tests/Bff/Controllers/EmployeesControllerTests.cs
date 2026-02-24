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

/// <summary>Tests for the employees controller.</summary>
public class EmployeesControllerTests
{
    private readonly EmployeeServiceClient _client;
    private readonly EmployeesController _controller;
    private HttpResponseMessage _nextResponse = new(HttpStatusCode.OK);

    /// <summary>Initializes a new instance of the <see cref="EmployeesControllerTests"/> class.</summary>
    public EmployeesControllerTests()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(_nextResponse));
        _client = new EmployeeServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        _controller = new EmployeesController(_client);
    }

    /// <summary>Sets the next HTTP response to be returned by the mock handler.</summary>
    private void SetResponse(HttpResponseMessage response) => _nextResponse = response;
    /// <summary>Sets the next HTTP response to be a JSON-serialized response with the given content and status.</summary>
    private void SetJsonResponse<T>(T content, HttpStatusCode status = HttpStatusCode.OK)
        => _nextResponse = new HttpResponseMessage(status) { Content = JsonContent.Create(content) };

    /// <summary>Verifies that Get returns OK.</summary>
    [Fact]
    public async Task Get_ReturnsOk()
    {
        SetJsonResponse(new PagedResponse<EmployeeSummaryDto>());
        var result = await _controller.Get();
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that GetById returns OK when the employee is found.</summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        SetJsonResponse(new EmployeeDetailDto());
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that GetById returns NotFound when the employee does not exist.</summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        SetResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>Verifies that Create returns a Created result when the employee is created successfully.</summary>
    [Fact]
    public async Task Create_WhenSucceeds_ReturnsCreated()
    {
        var id = Guid.NewGuid();
        SetJsonResponse(new EmployeeDetailDto { Id = id });
        var result = await _controller.Create(new CreateEmployeeRequest(), CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    /// <summary>Verifies that Update returns OK when the employee is updated successfully.</summary>
    [Fact]
    public async Task Update_WhenSucceeds_ReturnsOk()
    {
        SetJsonResponse(new EmployeeDetailDto());
        var result = await _controller.Update(Guid.NewGuid(), new UpdateEmployeeRequest(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that Terminate returns OK when the employee is terminated successfully.</summary>
    [Fact]
    public async Task Terminate_WhenSucceeds_ReturnsOk()
    {
        SetResponse(new HttpResponseMessage(HttpStatusCode.OK));
        var result = await _controller.Terminate(Guid.NewGuid(), new TerminateEmployeeRequest(), CancellationToken.None);
        Assert.IsType<OkResult>(result);
    }

    /// <summary>Verifies that GetAnalytics returns OK.</summary>
    [Fact]
    public async Task GetAnalytics_ReturnsOk()
    {
        SetJsonResponse(new HrAnalyticsDto());
        var result = await _controller.GetAnalytics(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that GetOrgChart returns OK.</summary>
    [Fact]
    public async Task GetOrgChart_ReturnsOk()
    {
        SetJsonResponse(new List<OrgNodeDto>());
        var result = await _controller.GetOrgChart(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    /// <summary>Verifies that AddNote returns a Created status when the note is added successfully.</summary>
    [Fact]
    public async Task AddNote_WhenSucceeds_ReturnsCreated()
    {
        SetResponse(new HttpResponseMessage(HttpStatusCode.Created));
        var result = await _controller.AddNote(Guid.NewGuid(), new AddEmployeeNoteRequest { Content = "Note" }, CancellationToken.None);
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(201, statusCodeResult.StatusCode);
    }
}

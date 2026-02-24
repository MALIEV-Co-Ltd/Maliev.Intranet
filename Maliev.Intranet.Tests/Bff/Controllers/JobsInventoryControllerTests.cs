using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class JobsControllerTests
{
    [Fact]
    public async Task GetById_ShouldReturnOk()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto { Id = jobId, OrderId = Guid.NewGuid(), MaterialId = Guid.NewGuid(), Technology = "FDM", Status = JobStatus.Pending, CreatedAt = DateTime.UtcNow };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(job) }));
        var client = new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new JobsController(client);
        var result = await controller.GetById(jobId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var client = new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new JobsController(client);
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetKanban_ShouldReturnOk()
    {
        var kanban = new KanbanResponse();
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(kanban) }));
        var client = new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new JobsController(client);
        var result = await controller.GetKanban(CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Start_ShouldReturnOk()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto { Id = jobId, OrderId = Guid.NewGuid(), MaterialId = Guid.NewGuid(), Technology = "FDM", Status = JobStatus.InProgress, CreatedAt = DateTime.UtcNow };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(job) }));
        var client = new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new JobsController(client);
        var result = await controller.Start(jobId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Queue_ShouldReturnOk()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto { Id = jobId, OrderId = Guid.NewGuid(), MaterialId = Guid.NewGuid(), Technology = "FDM", Status = JobStatus.Queued, AssignedMachineId = "PRUSA-01", CreatedAt = DateTime.UtcNow };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(job) }));
        var client = new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new JobsController(client);
        var result = await controller.Queue(jobId, new QueueJobRequest { MachineId = "PRUSA-01" }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Complete_ShouldReturnOk()
    {
        var jobId = Guid.NewGuid();
        var job = new JobDto { Id = jobId, OrderId = Guid.NewGuid(), MaterialId = Guid.NewGuid(), Technology = "FDM", Status = JobStatus.Completed, CompletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow.AddDays(-1) };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(job) }));
        var client = new JobServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new JobsController(client);
        var result = await controller.Complete(jobId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}

public class InventoryControllerTests
{
    [Fact]
    public async Task GetMaterialStatus_ShouldReturnOk()
    {
        var materialId = Guid.NewGuid();
        var status = new MaterialStatusSummary { MaterialId = materialId, ActiveBatches = 2, TotalRemainingGrams = 5000m, LowestBatchGrams = 200m, HasLowStockAlert = false };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(status) }));
        var client = new InventoryServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new InventoryController(client);
        var result = await controller.GetMaterialStatus(materialId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMaterialStatus_WhenNotFound_ShouldReturnNotFound()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var client = new InventoryServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new InventoryController(client);
        var result = await controller.GetMaterialStatus(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetActiveBatches_ShouldReturnOk()
    {
        var materialId = Guid.NewGuid();
        var batches = new List<InventoryBatchDto>
        {
            new() { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 1000m, RemainingWeightGrams = 800m, Status = BatchStatus.Active, Location = "SHELF-A1", ReceivedAt = DateTime.UtcNow }
        };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(batches) }));
        var client = new InventoryServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new InventoryController(client);
        var result = await controller.GetActiveBatches(materialId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateBatch_ShouldReturnCreatedAtAction()
    {
        var materialId = Guid.NewGuid();
        var batch = new InventoryBatchDto { Id = Guid.NewGuid(), MaterialId = materialId, InitialWeightGrams = 1000m, RemainingWeightGrams = 1000m, Status = BatchStatus.Active, Location = "SHELF-A1", ReceivedAt = DateTime.UtcNow };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(batch) }));
        var client = new InventoryServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new InventoryController(client);
        var result = await controller.CreateBatch(new CreateInventoryBatchRequest { MaterialId = materialId, InitialWeightGrams = 1000m, Location = "SHELF-A1" }, CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task CreateBatch_WhenBadRequest_ShouldReturnBadRequest()
    {
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        var client = new InventoryServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
        var controller = new InventoryController(client);
        var result = await controller.CreateBatch(new CreateInventoryBatchRequest(), CancellationToken.None);
        Assert.IsType<BadRequestResult>(result.Result);
    }
}

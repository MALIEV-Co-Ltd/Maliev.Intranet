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
/// Unit tests for <see cref="OrderLifecycleController"/> covering the BFF aggregation logic.
/// </summary>
public class OrderLifecycleControllerTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static readonly Guid OrderId = Guid.NewGuid();

    private static OrderDetailDto SampleOrder(string status = "Processing") => new()
    {
        Id          = OrderId,
        OrderNumber = "ORD-001",
        Status      = status,
        Timeline    =
        [
            new() { Status = "New",        Timestamp = DateTime.UtcNow.AddDays(-3) },
            new() { Status = "Processing", Timestamp = DateTime.UtcNow.AddDays(-2) }
        ]
    };

    /// <summary>Creates an OrderServiceClient that returns the given order.</summary>
    private static OrderServiceClient MakeOrderClient(OrderDetailDto? order)
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(order) }));
        return new OrderServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    /// <summary>Creates an OrderServiceClient that returns 404.</summary>
    private static OrderServiceClient MakeOrderClientNotFound()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        return new OrderServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    /// <summary>Creates an InvoiceServiceClient that returns the given invoice summary (via paged).</summary>
    private static InvoiceServiceClient MakeInvoiceClient(InvoiceSummaryDto? invoice)
    {
        var paged = new PagedResponse<InvoiceSummaryDto>
        {
            Data = invoice != null ? new List<InvoiceSummaryDto> { invoice } : new List<InvoiceSummaryDto>()
        };
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(paged) }));
        return new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    /// <summary>Creates an InvoiceServiceClient that returns 500 (simulates downstream failure).</summary>
    private static InvoiceServiceClient MakeInvoiceClientFailed()
    {
        var handler = new MockHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        return new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });
    }

    private static OrderLifecycleController Make(OrderServiceClient order, InvoiceServiceClient invoice) =>
        new(order, invoice);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_WhenOrderNotFound_ShouldReturn404()
    {
        var controller = Make(MakeOrderClientNotFound(), MakeInvoiceClientFailed());
        var result = await controller.Get(OrderId, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_WhenOrderExists_ShouldReturn7Stages()
    {
        var controller = Make(MakeOrderClient(SampleOrder()), MakeInvoiceClient(null));
        var result = await controller.Get(OrderId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<OrderLifecycleDto>(ok.Value);
        Assert.Equal(7, dto.Stages.Count);
    }

    [Fact]
    public async Task Get_WhenStatusProcessing_ConfirmedStageShouldBeCompleted()
    {
        var controller = Make(MakeOrderClient(SampleOrder("Processing")), MakeInvoiceClient(null));
        var result = await controller.Get(OrderId, CancellationToken.None);
        var dto = ((OkObjectResult)result.Result!).Value as OrderLifecycleDto;
        var confirmed = dto!.Stages.Single(s => s.Stage == "Confirmed");
        Assert.Equal("Completed", confirmed.Status);
    }

    [Fact]
    public async Task Get_WhenStatusNew_ConfirmedStageShouldBeCurrent()
    {
        var controller = Make(MakeOrderClient(SampleOrder("New")), MakeInvoiceClient(null));
        var result = await controller.Get(OrderId, CancellationToken.None);
        var dto = ((OkObjectResult)result.Result!).Value as OrderLifecycleDto;
        var confirmed = dto!.Stages.Single(s => s.Stage == "Confirmed");
        Assert.Equal("Current", confirmed.Status);
    }

    [Fact]
    public async Task Get_WhenInvoiceExists_InvoicedStageShouldHaveEntityReference()
    {
        var invoice = new InvoiceSummaryDto { Id = Guid.NewGuid(), InvoiceNumber = "INV-777", Status = "Sent" };
        var order   = SampleOrder("Delivered");
        order.CustomerPoNumber = "PO-001";
        var controller = Make(MakeOrderClient(order), MakeInvoiceClient(invoice));

        var result = await controller.Get(OrderId, CancellationToken.None);
        var dto = ((OkObjectResult)result.Result!).Value as OrderLifecycleDto;
        var invoiced = dto!.Stages.Single(s => s.Stage == "Invoiced");

        Assert.Equal("INV-777", invoiced.EntityReference);
        Assert.Equal("Completed", invoiced.Status);
    }

    [Fact]
    public async Task Get_WhenInvoiceServiceFails_ShouldDegradeGracefully()
    {
        // Invoice service down → InvoicedStage should be Pending, not throw
        var controller = Make(MakeOrderClient(SampleOrder("Processing")), MakeInvoiceClientFailed());
        var result = await controller.Get(OrderId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<OrderLifecycleDto>(ok.Value);
        // Must still return successfully with 7 stages
        Assert.Equal(7, dto.Stages.Count);
    }

    [Fact]
    public async Task Get_OrderNumberShouldBePropagatedToDto()
    {
        var controller = Make(MakeOrderClient(SampleOrder()), MakeInvoiceClient(null));
        var result = await controller.Get(OrderId, CancellationToken.None);
        var dto = ((OkObjectResult)result.Result!).Value as OrderLifecycleDto;
        Assert.Equal("ORD-001", dto!.OrderNumber);
    }

    [Fact]
    public async Task Get_CurrentStage_ShouldNotBeEmpty()
    {
        var controller = Make(MakeOrderClient(SampleOrder("QC")), MakeInvoiceClient(null));
        var result = await controller.Get(OrderId, CancellationToken.None);
        var dto = ((OkObjectResult)result.Result!).Value as OrderLifecycleDto;
        Assert.False(string.IsNullOrEmpty(dto!.CurrentStage));
    }

    [Fact]
    public async Task Get_WhenStatusPaid_AllStagesShouldBeCompletedExceptPaid()
    {
        var invoice = new InvoiceSummaryDto { Id = Guid.NewGuid(), InvoiceNumber = "INV-999", Status = "Paid" };
        var order   = SampleOrder("Paid");
        order.CustomerPoNumber = "PO-PAID";
        var controller = Make(MakeOrderClient(order), MakeInvoiceClient(invoice));

        var result = await controller.Get(OrderId, CancellationToken.None);
        var dto = ((OkObjectResult)result.Result!).Value as OrderLifecycleDto;

        var paid = dto!.Stages.Single(s => s.Stage == "Paid");
        Assert.Equal("Completed", paid.Status);
    }
}

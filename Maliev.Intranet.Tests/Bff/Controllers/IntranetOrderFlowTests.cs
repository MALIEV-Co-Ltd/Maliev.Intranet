using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>
/// Covers the Intranet-side of the complete manufacturing lifecycle end-to-end:
///
///   QuoteEngine BFF fast-tracks order states:
///     New → Reviewing → Reviewed → Quoted  (CreateOrder)
///     → Accepted                           (InitiatePayment)
///     → Paid                               (PaymentCompletedEventConsumer via MassTransit)
///
///   Intranet staff then completes the job:
///     Records payment            (PaymentsController)
///     Advances order status      (OrdersController)
///     Updates production job     (JobsController + SignalR broadcast)
///     Creates delivery note      (DeliveryNotesController)
///     Closes delivery note       (DeliveryNotesController.UpdateStatus → Delivered)
///     Views 7-stage lifecycle    (OrderLifecycleController)
/// </summary>
public sealed class IntranetOrderFlowTests
{
    // ── Factory helpers ───────────────────────────────────────────────────────

    private static OrderServiceClient MakeOrderClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new HttpClient(
            new MockHttpMessageHandler((req, _) => Task.FromResult(handler(req))))
        { BaseAddress = new Uri("http://order-test") });

    private static JobServiceClient MakeJobClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new HttpClient(
            new MockHttpMessageHandler((req, _) => Task.FromResult(handler(req))))
        { BaseAddress = new Uri("http://job-test") });

    private static PaymentServiceClient MakePaymentClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new HttpClient(
            new MockHttpMessageHandler((req, _) => Task.FromResult(handler(req))))
        { BaseAddress = new Uri("http://payment-test") });

    private static InvoiceServiceClient MakeInvoiceClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new HttpClient(
            new MockHttpMessageHandler((req, _) => Task.FromResult(handler(req))))
        { BaseAddress = new Uri("http://invoice-test") });

    private static UploadServiceClient MakeUploadClientStub() =>
        new(new HttpClient(
            new MockHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))))
        { BaseAddress = new Uri("http://upload-test") });

    private static (
        Mock<IHubContext<ProductionHub>> Hub,
        Mock<IClientProxy> AllProxy) MakeHubMocks()
    {
        var hub = new Mock<IHubContext<ProductionHub>>();
        var clients = new Mock<IHubClients>();
        var allProxy = new Mock<IClientProxy>();
        hub.Setup(h => h.Clients).Returns(clients.Object);
        clients.Setup(c => c.All).Returns(allProxy.Object);
        allProxy
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (hub, allProxy);
    }

    // ── PaymentsController ────────────────────────────────────────────────────

    /// <summary>
    /// Intranet finance staff records a payment for an order after the customer pays.
    /// The BFF proxies the request to PaymentService and returns 201 Created.
    /// </summary>
    [Fact]
    public async Task CreatePayment_ReturnsCreated_WhenPaymentServiceSucceeds()
    {
        var paymentClient = MakePaymentClient(
            _ => new HttpResponseMessage(HttpStatusCode.Created));
        var controller = new PaymentsController(paymentClient);

        var request = new CreatePaymentRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = 15_000m,
            PaymentMethod = "BankTransfer",
            PaymentDate = DateTime.UtcNow
        };

        var result = await controller.Create(request, CancellationToken.None);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(201, statusResult.StatusCode);
    }

    /// <summary>
    /// When PaymentService rejects the request, the BFF proxies the downstream error code.
    /// </summary>
    [Fact]
    public async Task CreatePayment_ProxiesDownstreamError_WhenPaymentServiceFails()
    {
        var paymentClient = MakePaymentClient(
            _ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity));
        var controller = new PaymentsController(paymentClient);

        var request = new CreatePaymentRequest
        {
            InvoiceId = Guid.NewGuid(),
            Amount = -1m,
            PaymentMethod = "Unknown",
            PaymentDate = DateTime.UtcNow
        };

        var result = await controller.Create(request, CancellationToken.None);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(422, statusResult.StatusCode);
    }

    // ── OrdersController ──────────────────────────────────────────────────────

    /// <summary>
    /// Intranet staff advances the order to "InProgress" once production begins.
    /// The BFF proxies the status update to OrderService and returns 204 No Content.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_ToInProgress_ReturnsNoContent_WhenSuccessful()
    {
        var orderClient = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var controller = new OrdersController(orderClient);

        var result = await controller.UpdateStatus(
            "ORD-2026-001",
            new UpdateOrderStatusRequest { Status = "InProgress" },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>
    /// QC release is the handoff that triggers OrderService's OrderCompletedEvent for DeliveryService.
    /// The BFF must preserve the employee audit note and customer-facing note on the downstream wire.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_ToQualityReleased_ForwardsAuditAndCustomerNotes()
    {
        string? downstreamJson = null;
        var orderClient = MakeOrderClient(
            request =>
            {
                downstreamJson = request.Content?.ReadAsStringAsync(CancellationToken.None).GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });
        var controller = new OrdersController(orderClient);

        var result = await controller.UpdateStatus(
            "ORD-2026-QC",
            new UpdateOrderStatusRequest
            {
                Status = "QualityReleased",
                InternalNotes = "QC release by operator after dimensional inspection passed.",
                CustomerNotes = "Your parts passed quality control and are being prepared for shipping."
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(downstreamJson);
        Assert.Contains("\"status\":\"QualityReleased\"", downstreamJson, StringComparison.Ordinal);
        Assert.Contains("\"internalNotes\":\"QC release by operator after dimensional inspection passed.\"", downstreamJson, StringComparison.Ordinal);
        Assert.Contains("\"customerNotes\":\"Your parts passed quality control and are being prepared for shipping.\"", downstreamJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// When OrderService rejects a status transition (e.g. invalid state machine transition),
    /// the BFF proxies the downstream error code.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_ProxiesDownstreamError_WhenOrderServiceFails()
    {
        var orderClient = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.Conflict));
        var controller = new OrdersController(orderClient);

        var result = await controller.UpdateStatus(
            "ORD-2026-001",
            new UpdateOrderStatusRequest { Status = "Delivered" },
            CancellationToken.None);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(409, statusResult.StatusCode);
    }

    /// <summary>
    /// Intranet order lists must preserve Make Studio quote/payment metadata from OrderService
    /// and expose a stable non-empty local key even when OrderService uses human-readable order IDs.
    /// </summary>
    [Fact]
    public async Task GetOrders_PreservesMakeStudioQuoteAndPaymentMetadata()
    {
        var orderJson = new
        {
            items = new[]
            {
                new
                {
                    orderId = "ORD-2026-QE-001",
                    customerId = Guid.NewGuid().ToString(),
                    customerType = "Customer",
                    currentStatus = "Paid",
                    paymentStatus = "Paid",
                    orderedQuantity = 2,
                    quotedAmount = 2500m,
                    quoteCurrency = "THB",
                    quoteNumber = "QT-2026-00042",
                    quoteVersionNumber = 3,
                    serviceCategoryName = "3D Printing",
                    processTypeName = "FDM",
                    isOutsourced = false,
                    createdAt = DateTime.UtcNow.AddHours(-2),
                    updatedAt = DateTime.UtcNow
                }
            },
            page = 1,
            pageSize = 20,
            totalCount = 1,
            totalPages = 1
        };

        var orderClient = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(orderJson)
            });
        var controller = new OrdersController(orderClient);

        var result = await controller.Get(ct: CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<PagedResponse<OrderSummaryDto>>(okResult.Value);
        var order = Assert.Single(page.Data);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("ORD-2026-QE-001", order.OrderNumber);
        Assert.Equal("Paid", order.Status);
        Assert.Equal("Paid", order.PaymentStatus);
        Assert.Equal("QT-2026-00042", order.QuoteNumber);
        Assert.Equal(3, order.QuoteVersionNumber);
    }

    /// <summary>
    /// Intranet order detail must preserve the accepted quote identity and payment state
    /// used by staff to trace a paid Make Studio order back to its formal quotation.
    /// </summary>
    [Fact]
    public async Task GetOrderById_PreservesAcceptedQuoteIdentityAndPaymentStatus()
    {
        var quoteId = Guid.NewGuid();
        var quoteVersionId = Guid.NewGuid();
        var orderJson = new
        {
            orderId = "ORD-2026-QE-DETAIL",
            customerId = Guid.NewGuid().ToString(),
            customerType = "Customer",
            currentStatus = "Paid",
            paymentStatus = "Paid",
            orderedQuantity = 1,
            quotedAmount = 1999m,
            quoteCurrency = "THB",
            quoteId,
            quoteNumber = "QT-2026-00099",
            quoteVersionId,
            quoteVersionNumber = 2,
            serviceCategoryName = "3D Printing",
            processTypeName = "SLS",
            isOutsourced = false,
            createdAt = DateTime.UtcNow.AddHours(-4),
            updatedAt = DateTime.UtcNow
        };

        var orderClient = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(orderJson)
            });
        var controller = new OrdersController(orderClient);

        var result = await controller.GetById("ORD-2026-QE-DETAIL", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var order = Assert.IsType<OrderDetailDto>(okResult.Value);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("ORD-2026-QE-DETAIL", order.OrderNumber);
        Assert.Equal("Paid", order.CurrentStatus);
        Assert.Equal("Paid", order.PaymentStatus);
        Assert.Equal(quoteId, order.QuoteId);
        Assert.Equal("QT-2026-00099", order.QuoteNumber);
        Assert.Equal(quoteVersionId, order.QuoteVersionId);
        Assert.Equal(2, order.QuoteVersionNumber);
    }

    // ── JobsController ────────────────────────────────────────────────────────

    /// <summary>
    /// When a production operator moves a job to "InProgress" on the Kanban board,
    /// the BFF updates the job status and broadcasts "JobStatusChanged" to all SignalR clients.
    /// </summary>
    [Fact]
    public async Task UpdateJobStatus_ToInProgress_ReturnsNoContent_AndBroadcastsJobStatusChanged()
    {
        var jobId = Guid.NewGuid();
        var jobClient = MakeJobClient(
            _ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var orderClientStub = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK));
        var (hubMock, allProxyMock) = MakeHubMocks();
        var controller = new JobsController(jobClient, orderClientStub, MakeUploadClientStub());

        var result = await controller.UpdateStatus(
            jobId,
            new UpdateJobStatusRequest { Status = "InProgress" },
            hubMock.Object,
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        allProxyMock.Verify(
            p => p.SendCoreAsync(
                "JobStatusChanged",
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// When JobService rejects a status transition, the BFF returns the downstream error code
    /// without broadcasting to SignalR.
    /// </summary>
    [Fact]
    public async Task UpdateJobStatus_ReturnsDownstreamStatusCode_WhenJobServiceFails()
    {
        var jobId = Guid.NewGuid();
        var jobClient = MakeJobClient(
            _ => new HttpResponseMessage(HttpStatusCode.UnprocessableEntity));
        var orderClientStub = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK));
        var (hubMock, allProxyMock) = MakeHubMocks();
        var controller = new JobsController(jobClient, orderClientStub, MakeUploadClientStub());

        var result = await controller.UpdateStatus(
            jobId,
            new UpdateJobStatusRequest { Status = "Completed" },
            hubMock.Object,
            CancellationToken.None);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(422, statusResult.StatusCode);
        allProxyMock.Verify(
            p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── DeliveryNotesController ───────────────────────────────────────────────

    /// <summary>
    /// When a delivery note is created for a shipped order, the BFF returns 201 Created
    /// with the newly-created delivery note detail.
    /// </summary>
    [Fact]
    public async Task CreateDeliveryNote_ReturnsCreatedWithDetail_WhenSuccessful()
    {
        var deliveryDetail = new DeliveryNoteDetailDto
        {
            Id = "DN-2026-000001",
            DeliveryNoteId = "DN-2026-000001",
            OrderNumber = "ORD-2026-001",
            Status = "Pending"
        };

        var deliveryClient = new Mock<IDeliveryServiceClient>();
        deliveryClient
            .Setup(c => c.CreateDeliveryNoteAsync(
                It.IsAny<CreateDeliveryNoteRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryDetail);

        var controller = new DeliveryNotesController(deliveryClient.Object);

        var request = new CreateDeliveryNoteRequest
        {
            OrderId = "ORD-2026-001",
            CustomerId = Guid.NewGuid(),
            DeliveryDate = DateTime.UtcNow.AddDays(3)
        };

        var actionResult = await controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var detail = Assert.IsType<DeliveryNoteDetailDto>(createdResult.Value);
        Assert.Equal("DN-2026-000001", detail.Id);
        Assert.Equal("ORD-2026-001", detail.OrderNumber);
    }

    /// <summary>
    /// When the customer receives and signs for the delivery, staff update the status to "Delivered".
    /// The BFF returns 200 OK with the updated delivery note.
    /// </summary>
    [Fact]
    public async Task UpdateDeliveryStatus_ToDelivered_ReturnsOk_WhenSuccessful()
    {
        var updatedDetail = new DeliveryNoteDetailDto
        {
            Id = "DN-2026-000001",
            DeliveryNoteId = "DN-2026-000001",
            OrderNumber = "ORD-2026-001",
            Status = "Delivered"
        };

        var deliveryClient = new Mock<IDeliveryServiceClient>();
        deliveryClient
            .Setup(c => c.UpdateDeliveryStatusAsync(
                "DN-2026-000001",
                It.IsAny<UpdateDeliveryStatusRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDetail);

        var controller = new DeliveryNotesController(deliveryClient.Object);

        var actionResult = await controller.UpdateStatus(
            "DN-2026-000001",
            new UpdateDeliveryStatusRequest { NewStatus = "Delivered" },
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var detail = Assert.IsType<DeliveryNoteDetailDto>(okResult.Value);
        Assert.Equal("Delivered", detail.Status);
    }

    /// <summary>
    /// When the delivery note ID does not exist, the BFF returns 404 Not Found.
    /// </summary>
    [Fact]
    public async Task UpdateDeliveryStatus_ReturnsNotFound_WhenDeliveryNoteNotFound()
    {
        var deliveryClient = new Mock<IDeliveryServiceClient>();
        deliveryClient
            .Setup(c => c.UpdateDeliveryStatusAsync(
                It.IsAny<string>(),
                It.IsAny<UpdateDeliveryStatusRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryNoteDetailDto?)null);

        var controller = new DeliveryNotesController(deliveryClient.Object);

        var actionResult = await controller.UpdateStatus(
            "DN-DOES-NOT-EXIST",
            new UpdateDeliveryStatusRequest { NewStatus = "Delivered" },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    // ── OrderLifecycleController ──────────────────────────────────────────────

    /// <summary>
    /// For an order that is currently "InProduction", the 7-stage lifecycle aggregator returns
    /// stages with "InProduction" as Current, all prior stages Completed, and all later stages Pending.
    /// </summary>
    [Fact]
    public async Task GetOrderLifecycle_ReturnsSevenStages_CurrentIsInProduction_WhenOrderIsInProduction()
    {
        var orderId = Guid.NewGuid();

        // OrderServiceClient reads a private OrderResponse shape.
        // Fields are camelCased; HttpClient.ReadFromJsonAsync uses case-insensitive matching.
        var orderJson = new
        {
            orderId = "ORD-2026-001",
            currentStatus = "InProduction",
            orderedQuantity = 1,
            quotedAmount = 15_000m,
            quoteCurrency = "THB",
            serviceCategoryName = "3D Printing",
            processTypeName = "FDM",
            isOutsourced = false,
            createdAt = DateTime.UtcNow.AddHours(-5),
            updatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var orderClient = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(orderJson)
            });

        // No CustomerPoNumber in the order response → invoice client is never called.
        var invoiceClient = MakeInvoiceClient(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var controller = new OrderLifecycleController(orderClient, invoiceClient);

        var result = await controller.Get(orderId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var lifecycle = Assert.IsType<OrderLifecycleDto>(okResult.Value);
        Assert.Equal(orderId, lifecycle.OrderId);
        Assert.Equal("ORD-2026-001", lifecycle.OrderNumber);
        Assert.Equal(7, lifecycle.Stages.Count);
        Assert.Equal("InProduction", lifecycle.CurrentStage);

        Assert.Equal("Completed", lifecycle.Stages.Single(s => s.Stage == "Quoted").Status);
        Assert.Equal("Completed", lifecycle.Stages.Single(s => s.Stage == "Confirmed").Status);
        Assert.Equal("Current", lifecycle.Stages.Single(s => s.Stage == "InProduction").Status);
        Assert.Equal("Pending", lifecycle.Stages.Single(s => s.Stage == "QC").Status);
        Assert.Equal("Pending", lifecycle.Stages.Single(s => s.Stage == "Delivered").Status);
        Assert.Equal("Pending", lifecycle.Stages.Single(s => s.Stage == "Invoiced").Status);
        Assert.Equal("Pending", lifecycle.Stages.Single(s => s.Stage == "Paid").Status);
    }

    /// <summary>
    /// InvoiceService marks automatically allocated payments as FullyPaid. The lifecycle BFF must
    /// treat that status as paid when resolving an order through its customer PO number.
    /// </summary>
    [Fact]
    public async Task GetOrderLifecycle_MarksPaidCompleted_WhenInvoiceStatusIsFullyPaid()
    {
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        HttpRequestMessage? invoiceRequest = null;

        var orderJson = new
        {
            orderId = "ORD-2026-PAID",
            currentStatus = "Invoiced",
            customerPoNumber = "PO-2026-PAID",
            orderedQuantity = 1,
            quotedAmount = 15_000m,
            quoteCurrency = "THB",
            serviceCategoryName = "3D Printing",
            processTypeName = "FDM",
            isOutsourced = false,
            createdAt = DateTime.UtcNow.AddHours(-5),
            updatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var invoiceJson = new
        {
            items = new[]
            {
                new
                {
                    id = invoiceId,
                    invoiceNumber = "INV-20260612-000001",
                    customerName = "Paid Customer Co.",
                    customerId = Guid.NewGuid(),
                    customerTaxId = "1234567890123",
                    billingAddress = "123 Test St",
                    poNumber = "PO-2026-PAID",
                    currency = "THB",
                    grandTotal = 15000m,
                    paidAmount = 15000m,
                    status = "FullyPaid",
                    issueDate = DateTime.UtcNow.Date,
                    dueDate = DateTime.UtcNow.Date.AddDays(30),
                    createdAt = DateTime.UtcNow.AddDays(-1),
                    updatedAt = DateTime.UtcNow
                }
            },
            page = 1,
            pageSize = 1,
            totalCount = 1
        };

        var orderClient = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(orderJson)
            });

        var invoiceClient = MakeInvoiceClient(
            request =>
            {
                invoiceRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(invoiceJson)
                };
            });

        var controller = new OrderLifecycleController(orderClient, invoiceClient);

        var result = await controller.Get(orderId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var lifecycle = Assert.IsType<OrderLifecycleDto>(okResult.Value);
        Assert.Equal("Completed", lifecycle.Stages.Single(s => s.Stage == "Invoiced").Status);
        Assert.Equal("Completed", lifecycle.Stages.Single(s => s.Stage == "Paid").Status);
        Assert.Equal(invoiceId, lifecycle.Stages.Single(s => s.Stage == "Invoiced").EntityId);
        Assert.NotNull(invoiceRequest);
        Assert.Contains("poNumber=PO-2026-PAID", invoiceRequest!.RequestUri!.PathAndQuery);
    }

    /// <summary>
    /// When the order does not exist in OrderService, the lifecycle endpoint returns 404 Not Found.
    /// </summary>
    [Fact]
    public async Task GetOrderLifecycle_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        var orderClient = MakeOrderClient(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var invoiceClient = MakeInvoiceClient(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var controller = new OrderLifecycleController(orderClient, invoiceClient);

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class PaymentsControllerTests
{
    [Fact]
    public async Task GetStats_WhenPaymentServiceReturnsNull_ReturnsEmptyStats()
    {
        var controller = new PaymentsController(MakeClient(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await controller.GetStats();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PaymentStatsDto>(ok.Value);
        Assert.Equal(0m, dto.TodayTotal);
        Assert.Equal(0, dto.PendingCount);
    }

    [Fact]
    public async Task Get_WhenPaymentServiceDoesNotExposeList_ReturnsEmptyPage()
    {
        var controller = new PaymentsController(MakeClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await controller.Get(page: 2, pageSize: 50);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PagedResponse<PaymentSummaryDto>>(ok.Value);
        Assert.Empty(dto.Data);
    }

    [Fact]
    public async Task GetById_WhenPaymentIsMissing_ReturnsNotFound()
    {
        var controller = new PaymentsController(MakeClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await controller.GetById(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Theory]
    [InlineData(nameof(PaymentsController.GetStats), MalievPermissions.Payment.Read)]
    [InlineData(nameof(PaymentsController.Get), MalievPermissions.Payment.Read)]
    [InlineData(nameof(PaymentsController.GetById), MalievPermissions.Payment.Read)]
    [InlineData(nameof(PaymentsController.Create), MalievPermissions.Payment.Write)]
    [InlineData(nameof(PaymentsController.Allocate), MalievPermissions.Payment.Write)]
    [InlineData(nameof(PaymentsController.Void), MalievPermissions.Payment.Write)]
    public void PaymentEndpoints_RequirePaymentPermissions(string actionName, string expectedPermission)
    {
        var method = typeof(PaymentsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == actionName);

        var attribute = Assert.Single(method.GetCustomAttributes<RequirePermissionAttribute>());

        Assert.Contains(expectedPermission, attribute.Policy, StringComparison.Ordinal);
        Assert.Equal("Bearer,Cookies", attribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task PaymentServiceClient_ReadsStatsFromPaymentMetricsEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PaymentStatsDto
                {
                    TodayTotal = 1200m,
                    PendingCount = 3,
                    MonthTotal = 5000m
                })
            };
        });

        var result = await client.GetPaymentStatsAsync();

        Assert.NotNull(result);
        Assert.Equal("/payment/v1/metrics/stats", capturedRequest?.RequestUri?.PathAndQuery);
        Assert.Equal(1200m, result.TodayTotal);
        Assert.Equal(3, result.PendingCount);
        Assert.Equal(5000m, result.MonthTotal);
    }

    [Fact]
    public async Task PaymentServiceClient_ReadsPagedPaymentsFromPaymentEndpoint()
    {
        var paymentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PagedResponse<PaymentSummaryDto>
                {
                    Data =
                    [
                        new PaymentSummaryDto
                        {
                            Id = paymentId,
                            PaymentNumber = "PAY-2026-001",
                            InvoiceNumber = "INV-2026-001",
                            CustomerName = "MALIEV Customer",
                            Amount = 2675m,
                            PaymentMethod = "Bank Transfer",
                            Status = "Completed"
                        }
                    ],
                    Meta = new PaginationMeta { CurrentPage = 2, PageSize = 50, TotalCount = 1 }
                })
            };
        });

        var result = await client.GetPaymentsAsync(page: 2, pageSize: 50);

        Assert.NotNull(result);
        Assert.Equal("/payment/v1/payments?page=2&pageSize=50", capturedRequest?.RequestUri?.PathAndQuery);
        var payment = Assert.Single(result.Data);
        Assert.Equal(paymentId, payment.Id);
        Assert.Equal("PAY-2026-001", payment.PaymentNumber);
        Assert.Equal("Completed", payment.Status);
    }

    [Fact]
    public async Task PaymentServiceClient_ReadsPaymentDetailFromPaymentEndpoint()
    {
        var paymentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new PaymentDetailDto
                {
                    Id = paymentId,
                    PaymentNumber = "PAY-2026-001",
                    Amount = 2675m,
                    Currency = "THB",
                    PaymentMethod = "Bank Transfer",
                    Status = "Completed",
                    TransactionId = "txn_123"
                })
            };
        });

        var result = await client.GetPaymentByIdAsync(paymentId);

        Assert.NotNull(result);
        Assert.Equal($"/payment/v1/payments/{paymentId}", capturedRequest?.RequestUri?.PathAndQuery);
        Assert.Equal(paymentId, result.Id);
        Assert.Equal("txn_123", result.TransactionId);
    }

    [Fact]
    public async Task PaymentServiceClient_CreatesPaymentThroughPaymentEndpoint()
    {
        JsonDocument? capturedPayload = null;
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(async (request, _) =>
        {
            capturedRequest = request;
            capturedPayload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var invoiceId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var response = await client.CreatePaymentAsync(new CreatePaymentRequest
        {
            InvoiceId = invoiceId,
            Amount = 2675m,
            PaymentMethod = "Bank Transfer",
            PaymentDate = DateTime.Parse("2026-05-03T00:00:00Z").ToUniversalTime()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(HttpMethod.Post, capturedRequest?.Method);
        Assert.Equal("/payment/v1/payments", capturedRequest?.RequestUri?.PathAndQuery);
        Assert.NotNull(capturedPayload);
        Assert.Equal(invoiceId, capturedPayload.RootElement.GetProperty("invoiceId").GetGuid());
        Assert.Equal(2675m, capturedPayload.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal("Bank Transfer", capturedPayload.RootElement.GetProperty("paymentMethod").GetString());
    }

    [Fact]
    public async Task AllocateAndVoid_WhenNotExposedByPaymentService_ReturnNotFound()
    {
        var controller = new PaymentsController(MakeClient(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var paymentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var allocate = await controller.Allocate(paymentId, new AllocatePaymentRequest(), CancellationToken.None);
        var voidResult = await controller.Void(paymentId, new VoidPaymentRequest { Reason = "Duplicate payment" }, CancellationToken.None);

        var allocateStatus = Assert.IsType<StatusCodeResult>(allocate);
        var voidStatus = Assert.IsType<StatusCodeResult>(voidResult);
        Assert.Equal(404, allocateStatus.StatusCode);
        Assert.Equal(404, voidStatus.StatusCode);
    }

    private static PaymentServiceClient MakeClient(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        MakeClient((request, _) => Task.FromResult(handler(request)));

    private static PaymentServiceClient MakeClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new HttpClient(new MockHttpMessageHandler(handler)) { BaseAddress = new Uri("http://payment-test") });
}

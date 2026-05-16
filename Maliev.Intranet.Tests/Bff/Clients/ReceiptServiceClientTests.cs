using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public sealed class ReceiptServiceClientTests
{
    [Fact]
    public async Task CreateReceiptAsync_PostsReceiptServiceContractAndMapsResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        JsonDocument? capturedPayload = null;
        var invoiceId = Guid.Parse("4676616d-9264-46ad-a8be-7058682d183b");
        var receiptId = Guid.Parse("20f2c0c8-254f-4b82-b508-dfdbce1fb3cb");
        var client = MakeClient(async (request, ct) =>
        {
            capturedRequest = request;
            capturedPayload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));

            return JsonContent.Create(new
            {
                id = receiptId,
                receiptNumber = "MALIEV-2026-000001",
                invoiceId,
                issueDate = new DateTime(2026, 5, 3, 9, 15, 0, DateTimeKind.Utc),
                customerName = "Paid Customer Co.",
                totalAmount = 2675m,
                paymentMethod = "Bank Transfer",
                status = "PendingPdf",
                createdAt = new DateTime(2026, 5, 3, 9, 15, 1, DateTimeKind.Utc),
                createdBy = "employee@maliev.local",
                lineItems = new[]
                {
                    new { description = "Machining", lineTotal = 2675m }
                }
            });
        });

        var result = await client.CreateReceiptAsync(new CreateReceiptRequest
        {
            InvoiceId = invoiceId,
            Amount = 2675m,
            PaymentMethod = "Bank Transfer"
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/receipt/v1/receipts", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(capturedPayload);
        Assert.Equal(invoiceId, capturedPayload.RootElement.GetProperty("invoiceId").GetGuid());
        Assert.Equal(2675m, capturedPayload.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal("Bank Transfer", capturedPayload.RootElement.GetProperty("paymentMethod").GetString());
        Assert.NotNull(result);
        Assert.Equal(receiptId, result.Id);
        Assert.Equal(invoiceId, result.InvoiceId);
        Assert.Equal("MALIEV-2026-000001", result.ReceiptNumber);
        Assert.Equal("PendingPdf", result.Status);
        Assert.Equal(2675m, result.TotalAmount);
        var line = Assert.Single(result.Lines);
        Assert.Equal("Machining", line.Description);
        Assert.Equal(2675m, line.Amount);
    }

    private static ReceiptServiceClient MakeClient(Func<HttpRequestMessage, CancellationToken, Task<HttpContent>> contentFactory)
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = await contentFactory(request, ct)
            });

        return new ReceiptServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://receipt-service")
        });
    }
}

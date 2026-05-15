using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public sealed class InvoiceServiceClientTests
{
    [Fact]
    public async Task GetInvoicesAsync_MapsInvoiceServicePaginationAndTotals()
    {
        HttpRequestMessage? capturedRequest = null;
        var issueDate = new DateTime(2026, 5, 1);
        var dueDate = new DateTime(2026, 5, 31);
        var createdAt = new DateTime(2026, 5, 2, 8, 30, 0, DateTimeKind.Utc);
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new
            {
                items = new[]
                {
                    new
                    {
                        id = Guid.Parse("7a3858bf-5b7d-477d-a1cc-7b4764678a2d"),
                        invoiceNumber = "INV-2026-0001",
                        customerName = "MALIEV Customer Co.",
                        grandTotal = 12450.25m,
                        issueDate,
                        dueDate,
                        status = "Draft",
                        createdAt
                    }
                },
                page = 2,
                pageSize = 10,
                totalCount = 21,
                totalPages = 3
            });
        });

        var result = await client.GetInvoicesAsync(page: 2, pageSize: 10);

        Assert.NotNull(capturedRequest);
        Assert.Equal("/invoice/v1/invoices?page=2&pageSize=10", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal(2, result.Meta.CurrentPage);
        Assert.Equal(3, result.Meta.TotalPages);
        Assert.Equal(21, result.Meta.TotalCount);
        var invoice = Assert.Single(result.Data);
        Assert.Equal("INV-2026-0001", invoice.InvoiceNumber);
        Assert.Equal("MALIEV Customer Co.", invoice.CustomerName);
        Assert.Equal(12450.25m, invoice.Total);
        Assert.Equal(12450.25m, invoice.Balance);
        Assert.Equal(issueDate, invoice.IssueDate);
        Assert.Equal(dueDate, invoice.DueDate);
        Assert.Equal(createdAt, invoice.CreatedAt);
    }

    [Fact]
    public async Task CreateInvoiceAsync_PostsInvoiceServiceContract()
    {
        JsonDocument? capturedPayload = null;
        var client = MakeClient(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/invoice/v1/invoices", request.RequestUri!.PathAndQuery);
            var payload = await request.Content!.ReadAsStringAsync(ct);
            capturedPayload = JsonDocument.Parse(payload);

            return JsonContent.Create(new
            {
                id = Guid.Parse("861e06b0-8531-44bd-b390-ea458ac7deba"),
                invoiceNumber = "INV-2026-0002",
                customerName = "Apex Robotics",
                grandTotal = 5350m,
                issueDate = new DateTime(2026, 5, 3),
                dueDate = new DateTime(2026, 6, 2),
                status = "Draft",
                createdAt = new DateTime(2026, 5, 3, 12, 0, 0, DateTimeKind.Utc)
            });
        });

        var result = await client.CreateInvoiceAsync(new CreateInvoiceRequest
        {
            CustomerId = Guid.Parse("efcb0881-e6be-4a81-af52-4d3f2d7df0fa"),
            BillingIdentityType = BillingIdentityType.Corporate,
            CustomerName = "Apex Robotics",
            CustomerTaxId = "0125561001573",
            BillingAddress = "88 Test Road, Bangkok 10110",
            ShippingAddress = "Warehouse 2, Bangkok 10120",
            PoNumber = "PO-7788",
            Currency = "THB",
            IssueDate = new DateTime(2026, 5, 3),
            DueDate = new DateTime(2026, 6, 2),
            PaymentTermsDays = 30,
            Items =
            [
                new InvoiceItemDto
                {
                    Description = "Machined aluminum bracket",
                    Quantity = 2,
                    UnitPrice = 2500m,
                    TaxRate = 7m
                }
            ]
        });

        Assert.NotNull(result);
        Assert.Equal("INV-2026-0002", result.InvoiceNumber);
        Assert.NotNull(capturedPayload);
        var root = capturedPayload.RootElement;
        Assert.Equal("Apex Robotics", root.GetProperty("customerName").GetString());
        Assert.Equal("0125561001573", root.GetProperty("customerTaxId").GetString());
        Assert.Equal("88 Test Road, Bangkok 10110", root.GetProperty("billingAddress").GetString());
        Assert.Equal("PO-7788", root.GetProperty("poNumber").GetString());
        Assert.False(root.TryGetProperty("items", out _));
        var line = root.GetProperty("lines")[0];
        Assert.Equal(1, line.GetProperty("lineNumber").GetInt32());
        Assert.Equal("Machined aluminum bracket", line.GetProperty("description").GetString());
        Assert.Equal(2m, line.GetProperty("quantity").GetDecimal());
        Assert.Equal(2500m, line.GetProperty("unitPrice").GetDecimal());
        Assert.Equal("VAT", line.GetProperty("taxCategory").GetString());
        Assert.Equal(7m, line.GetProperty("taxRate").GetDecimal());
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_MapsInvoiceServiceLinesToDetailItems()
    {
        var invoiceId = Guid.Parse("ed2047ed-2a41-4f8e-a24f-d0db6f73ea01");
        HttpRequestMessage? capturedRequest = null;
        var client = MakeClient(request =>
        {
            capturedRequest = request;
            return JsonContent.Create(new
            {
                id = invoiceId,
                customerId = Guid.Parse("5bdd89ee-7478-4a5b-aa74-e7e90a9446ac"),
                invoiceNumber = "INV-2026-0003",
                customerName = "Nexus Manufacturing",
                customerTaxId = "0105556000001",
                billingAddress = "22 Industrial Road",
                shippingAddress = "Dock A",
                poNumber = "PO-9001",
                currency = "THB",
                subtotal = 1000m,
                taxAmount = 70m,
                withholdingTaxAmount = 0m,
                grandTotal = 1070m,
                issueDate = new DateTime(2026, 5, 3),
                dueDate = new DateTime(2026, 6, 2),
                paymentTermsDays = 30,
                status = "Draft",
                createdAt = new DateTime(2026, 5, 3, 9, 0, 0, DateTimeKind.Utc),
                updatedAt = new DateTime(2026, 5, 3, 9, 5, 0, DateTimeKind.Utc),
                lines = new[]
                {
                    new
                    {
                        description = "CNC service",
                        quantity = 1m,
                        unitPrice = 1000m,
                        taxRate = 7m
                    }
                }
            });
        });

        var result = await client.GetInvoiceByIdAsync(invoiceId);

        Assert.NotNull(capturedRequest);
        Assert.Equal($"/invoice/v1/invoices/{invoiceId}", capturedRequest.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
        Assert.Equal("INV-2026-0003", result.InvoiceNumber);
        Assert.Equal(1000m, result.SubTotal);
        Assert.Equal(1070m, result.Total);
        var line = Assert.Single(result.Items);
        Assert.Equal("CNC service", line.Description);
        Assert.Equal(1m, line.Quantity);
        Assert.Equal(1000m, line.UnitPrice);
        Assert.Equal(7m, line.TaxRate);
    }

    [Fact]
    public async Task FinalizeInvoiceAsync_PostsRequiredFinalizedByPayload()
    {
        var invoiceId = Guid.Parse("8a071819-a277-4c7d-914d-45a0ac2602f2");
        JsonDocument? capturedPayload = null;
        var client = MakeClient(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal($"/invoice/v1/invoices/{invoiceId}/finalize", request.RequestUri!.PathAndQuery);
            var payload = await request.Content!.ReadAsStringAsync(ct);
            capturedPayload = JsonDocument.Parse(payload);

            return JsonContent.Create(new
            {
                id = invoiceId,
                invoiceNumber = "INV-2026-0004",
                status = "Finalized"
            });
        });

        var result = await client.FinalizeInvoiceAsync(invoiceId);

        Assert.True(result);
        Assert.NotNull(capturedPayload);
        Assert.Equal("Maliev.Intranet", capturedPayload.RootElement.GetProperty("finalizedBy").GetString());
    }

    private static InvoiceServiceClient MakeClient(Func<HttpRequestMessage, HttpContent> contentFactory)
    {
        var handler = new MockHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = contentFactory(request)
            }));

        return new InvoiceServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://invoice-service")
        });
    }

    private static InvoiceServiceClient MakeClient(Func<HttpRequestMessage, CancellationToken, Task<HttpContent>> contentFactory)
    {
        var handler = new MockHttpMessageHandler(async (request, ct) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = await contentFactory(request, ct)
            });

        return new InvoiceServiceClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://invoice-service")
        });
    }
}

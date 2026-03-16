using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class InvoiceServiceClientTests
{
    [Fact]
    public async Task GetInvoicesAsync_ShouldReturnPagedResponse()
    {
        var response = new PagedResponse<InvoiceSummaryDto>
        {
            Data = new List<InvoiceSummaryDto> { new() { Id = Guid.NewGuid(), InvoiceNumber = "INV-001" } }
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.GetInvoicesAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetInvoicesAsync_ShouldPassPageAndSizeParameters()
    {
        string? capturedUrl = null;
        var response = new PagedResponse<InvoiceSummaryDto>();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedUrl = req.RequestUri?.ToString();
            return Task.FromResult(httpResponse);
        });
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        await client.GetInvoicesAsync(page: 2, pageSize: 10);

        Assert.NotNull(capturedUrl);
        Assert.Contains("page=2", capturedUrl);
        Assert.Contains("pageSize=10", capturedUrl);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_ShouldReturnInvoice_WhenFound()
    {
        var invoice = new InvoiceDetailDto { Id = Guid.NewGuid(), InvoiceNumber = "INV-001" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(invoice) };
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.GetInvoiceByIdAsync(invoice.Id);

        Assert.NotNull(result);
        Assert.Equal(invoice.InvoiceNumber, result.InvoiceNumber);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.GetInvoiceByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldSendPostRequest_AndReturnResponse()
    {
        string? capturedMethod = null;
        var created = new InvoiceSummaryDto { Id = Guid.NewGuid(), InvoiceNumber = "INV-002" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(created) };
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedMethod = req.Method.Method;
            return Task.FromResult(httpResponse);
        });
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        var result = await client.CreateInvoiceAsync(request);

        Assert.Equal("POST", capturedMethod);
        Assert.NotNull(result);
        Assert.Equal(created.InvoiceNumber, result.InvoiceNumber);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldReturnNull_WhenDownstreamFails()
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.CreateInvoiceAsync(new CreateInvoiceRequest());

        Assert.Null(result);
    }

    [Fact]
    public async Task FinalizeInvoiceAsync_ShouldSendPostRequest_AndReturnTrue_WhenSuccessful()
    {
        string? capturedMethod = null;
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedMethod = req.Method.Method;
            return Task.FromResult(httpResponse);
        });
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.FinalizeInvoiceAsync(Guid.NewGuid());

        Assert.Equal("POST", capturedMethod);
        Assert.True(result);
    }

    [Fact]
    public async Task FinalizeInvoiceAsync_ShouldReturnFalse_WhenDownstreamFails()
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity);
        var handler = new MockHttpMessageHandler((req, ct) => Task.FromResult(httpResponse));
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.FinalizeInvoiceAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateInvoiceAsync_ShouldSendPutRequest_AndReturnUpdatedInvoice()
    {
        string? capturedMethod = null;
        var updated = new InvoiceDetailDto { Id = Guid.NewGuid(), InvoiceNumber = "INV-001" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(updated) };
        var handler = new MockHttpMessageHandler((req, ct) =>
        {
            capturedMethod = req.Method.Method;
            return Task.FromResult(httpResponse);
        });
        var client = new InvoiceServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") });

        var result = await client.UpdateInvoiceAsync(updated.Id, new UpdateInvoiceRequest { Notes = "Updated" });

        Assert.Equal("PUT", capturedMethod);
        Assert.NotNull(result);
    }
}

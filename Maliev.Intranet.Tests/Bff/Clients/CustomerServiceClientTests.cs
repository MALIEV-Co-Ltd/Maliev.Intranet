using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class CustomerServiceClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly CustomerServiceClient _client;

    public CustomerServiceClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://test")
        };
        var logger = new Mock<ILogger<CustomerServiceClient>>().Object;
        _client = new CustomerServiceClient(httpClient, logger);
    }

    [Fact]
    public async Task CreateCustomerBasicAsync_ShouldWork()
    {
        var request = new CustomerOnboardingRequest
        {
            Customer = new CreateCustomerRequest { FirstName = "John", LastName = "Doe" },
            InternalNote = "Note"
        };

        // 1. Mock Customer Creation
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains("/customers")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CustomerResponse { Id = Guid.NewGuid() })
            });

        // 2. Mock Note Creation
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains("/internal-notes")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await _client.CreateCustomerBasicAsync(request);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_ShouldAggregateData()
    {
        var customerId = Guid.NewGuid();
        var customer = new CustomerDetailDto { Id = customerId, FirstName = "John" };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains($"/customers/{customerId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(customer)
            });

        // Mock other related calls
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains("ownerId=")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new List<object>()) });

        var result = await _client.GetCustomerByIdAsync(customerId);

        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task GetCustomersAsync_ShouldReturnPagedData()
    {
        var response = new
        {
            items = new List<CustomerSummaryDto> { new() { Name = "Test" } },
            totalCount = 1,
            page = 1,
            pageSize = 10,
            totalPages = 1
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.PathAndQuery.Contains("/customers")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });

        var result = await _client.GetCustomersAsync();

        Assert.NotNull(result);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task UpdateAddressAsync_ShouldReturnTrue()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await _client.UpdateAddressAsync(Guid.NewGuid(), new UpdateAddressRequest { Version = new byte[0] });

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteDocumentAsync_ShouldReturnTrue()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await _client.DeleteDocumentAsync(Guid.NewGuid(), new byte[0]);

        Assert.True(result);
    }

    [Fact]
    public async Task GetNdaHistoryAsync_ShouldReturnData()
    {
        var response = new List<NDAAuditLogResponse> { new() { Action = "Signed" } };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });

        var result = await _client.GetNdaHistoryAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Single(result);
    }
}

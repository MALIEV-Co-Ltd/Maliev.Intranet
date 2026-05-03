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
    public async Task CreateAddressesAsync_WithBillingOnlyAndNoExistingShipping_CreatesDefaultShippingAddress()
    {
        var customerId = Guid.NewGuid();
        var countryId = Guid.NewGuid();
        var postedAddressPayloads = new List<string>();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Get &&
                    m.RequestUri!.PathAndQuery.Contains($"/addresses?ownerType=Customer&ownerId={customerId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<AddressResponse>())
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Post &&
                    m.RequestUri!.PathAndQuery.Contains("/addresses")),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage message, CancellationToken _) =>
            {
                postedAddressPayloads.Add(await message.Content!.ReadAsStringAsync());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AddressResponse { Id = Guid.NewGuid() })
                };
            });

        var result = await _client.CreateAddressesAsync(customerId,
        [
            new CreateAddressRequest
            {
                Type = "Billing",
                IsDefault = true,
                AddressLine1 = "36/1 Moo 3",
                AddressLine2 = "Unit A",
                AddressLine3 = "Building B",
                District = "Khlong Khoi",
                City = "Pak Kret",
                StateProvince = "Nonthaburi",
                PostalCode = "11120",
                CountryId = countryId,
                RecipientName = "Natthaphon",
                RecipientPhone = "028816002"
            }
        ]);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, postedAddressPayloads.Count);

        using var billingPayload = System.Text.Json.JsonDocument.Parse(postedAddressPayloads[0]);
        using var shippingPayload = System.Text.Json.JsonDocument.Parse(postedAddressPayloads[1]);
        var billingRoot = billingPayload.RootElement;
        var shippingRoot = shippingPayload.RootElement;

        Assert.Equal("Billing", billingRoot.GetProperty("type").GetString());
        Assert.Equal("Shipping", shippingRoot.GetProperty("type").GetString());
        Assert.True(shippingRoot.GetProperty("isDefault").GetBoolean());
        Assert.Equal(billingRoot.GetProperty("addressLine1").GetString(), shippingRoot.GetProperty("addressLine1").GetString());
        Assert.Equal(billingRoot.GetProperty("addressLine2").GetString(), shippingRoot.GetProperty("addressLine2").GetString());
        Assert.Equal(billingRoot.GetProperty("addressLine3").GetString(), shippingRoot.GetProperty("addressLine3").GetString());
        Assert.Equal(billingRoot.GetProperty("district").GetString(), shippingRoot.GetProperty("district").GetString());
        Assert.Equal(billingRoot.GetProperty("city").GetString(), shippingRoot.GetProperty("city").GetString());
        Assert.Equal(billingRoot.GetProperty("stateProvince").GetString(), shippingRoot.GetProperty("stateProvince").GetString());
        Assert.Equal(billingRoot.GetProperty("postalCode").GetString(), shippingRoot.GetProperty("postalCode").GetString());
        Assert.Equal(billingRoot.GetProperty("countryId").GetString(), shippingRoot.GetProperty("countryId").GetString());
        Assert.Equal(billingRoot.GetProperty("recipientName").GetString(), shippingRoot.GetProperty("recipientName").GetString());
        Assert.Equal(billingRoot.GetProperty("recipientPhone").GetString(), shippingRoot.GetProperty("recipientPhone").GetString());
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
    public async Task GetCustomersAsync_ForwardsPaginationAndSupportedFilters()
    {
        var response = new
        {
            items = new List<CustomerSummaryDto> { new() { Name = "Test" } },
            totalCount = 1,
            page = 2,
            pageSize = 10,
            totalPages = 4
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.RequestUri!.PathAndQuery == "/customer/v1/customers?page=2&pageSize=10&sortBy=createdAt&sortDirection=desc&query=acme&segment=Enterprise&tier=VIP&includeDeleted=true"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });

        var result = await _client.GetCustomersAsync("acme", "Enterprise", "VIP", includeDeleted: true, page: 2, pageSize: 10);

        Assert.NotNull(result);
        Assert.Equal(2, result.Meta.CurrentPage);
        Assert.Equal(10, result.Meta.PageSize);
        Assert.Equal(4, result.Meta.TotalPages);
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

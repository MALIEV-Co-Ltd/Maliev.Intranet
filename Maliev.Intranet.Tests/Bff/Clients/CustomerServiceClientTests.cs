using System.Net;
using System.Net.Http.Json;
using System.Text;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Tests.Testing;
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
    public async Task SearchCompanyResultsAsync_MapsCustomerServiceWireShape()
    {
        var companyId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/customer/v1/companies/search?query=ABC&limit=8", request.RequestUri!.PathAndQuery);
            const string json = """
            [
              {
                "id": "__COMPANY_ID__",
                "name": "ABC Manufacturing",
                "vatNumber": "1234567890123",
                "segment": "Enterprise",
                "source": 0,
                "billingAddress": {
                  "isDefault": true,
                  "addressLine1": "88 Test Road",
                  "city": "Bangkok",
                  "stateProvince": "Bangkok",
                  "postalCode": "10110"
                }
              }
            ]
            """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json.Replace("__COMPANY_ID__", companyId.ToString()), Encoding.UTF8, "application/json")
            });
        });
        var client = new CustomerServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }, new Mock<ILogger<CustomerServiceClient>>().Object);

        var results = await client.SearchCompanyResultsAsync("ABC", 8);

        var result = Assert.Single(results);
        Assert.Equal(companyId, result.Id);
        Assert.Equal("ABC Manufacturing", result.Name);
        Assert.Equal("1234567890123", result.VatNumber);
        Assert.Equal("Internal", result.Source);
        Assert.NotNull(result.DefaultBillingAddress);
        Assert.Equal("88 Test Road", result.DefaultBillingAddress.AddressLine1);
        Assert.Equal("Bangkok", result.DefaultBillingAddress.StateProvince);
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
    public async Task CreateCustomerBasicAsync_WithOnboardingPayload_CreatesRelatedRecords()
    {
        var customerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var countryId = Guid.NewGuid();
        var calls = new List<string>();
        var addressPayloads = new List<string>();
        var documentPayload = string.Empty;
        var ndaStatusPayload = string.Empty;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            calls.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");

            if (request.Method == HttpMethod.Post && request.RequestUri.PathAndQuery == "/customer/v1/companies")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new CompanyResponse { Id = companyId, Name = "บริษัท ทดสอบ จำกัด" })
                });
            }

            if (request.Method == HttpMethod.Post && request.RequestUri.PathAndQuery == "/customer/v1/customers")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new CustomerResponse { Id = customerId })
                });
            }

            if (request.Method == HttpMethod.Get && request.RequestUri.PathAndQuery.Contains("/customer/v1/addresses?ownerType=Customer", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new List<AddressResponse>())
                });
            }

            if (request.Method == HttpMethod.Post && request.RequestUri.PathAndQuery == "/customer/v1/addresses")
            {
                addressPayloads.Add(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AddressResponse { Id = Guid.NewGuid() })
                });
            }

            if (request.Method == HttpMethod.Post && request.RequestUri.PathAndQuery == "/customer/v1/documents")
            {
                documentPayload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new DocumentResponse
                    {
                        Id = Guid.NewGuid(),
                        DocumentCategory = DocumentCategories.NDA,
                        DocumentSubType = "Signed"
                    })
                });
            }

            if (request.Method == HttpMethod.Post && request.RequestUri.PathAndQuery == "/customer/v1/ndas")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new NDAResponse { Id = Guid.NewGuid(), Xmin = 42 })
                });
            }

            if (request.Method == HttpMethod.Patch && request.RequestUri.PathAndQuery.Contains("/customer/v1/ndas/", StringComparison.Ordinal))
            {
                ndaStatusPayload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            if (request.Method == HttpMethod.Post && request.RequestUri.PathAndQuery == "/customer/v1/internal-notes")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var client = new CustomerServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }, new Mock<ILogger<CustomerServiceClient>>().Object);

        var result = await client.CreateCustomerBasicAsync(new CustomerOnboardingRequest
        {
            Customer = new CreateCustomerRequest
            {
                FirstName = "Somchai",
                LastName = "Mali",
                Email = "somchai@example.com"
            },
            NewCompany = new CreateCompanyRequest { Name = "บริษัท ทดสอบ จำกัด", VatNumber = "1234567890123" },
            Addresses =
            [
                new CreateAddressRequest
                {
                    Type = "Billing",
                    AddressLine1 = "1 Silom",
                    City = "Bang Rak",
                    StateProvince = "Bangkok",
                    PostalCode = "10500",
                    CountryId = countryId
                }
            ],
            CompanyBillingAddress = new CreateAddressRequest
            {
                Type = "Billing",
                AddressLine1 = "99 Company Tower",
                City = "Bang Rak",
                StateProvince = "Bangkok",
                PostalCode = "10500",
                CountryId = countryId
            },
            Documents =
            [
                new CreateDocumentRequest
                {
                    DocumentCategory = DocumentCategories.NDA,
                    DocumentSubType = "Signed",
                    FileReference = "upload-1",
                    FileName = "nda.pdf",
                    FileSize = 100,
                    MimeType = "application/pdf"
                }
            ],
            Nda = new CreateNDARequest { IsActive = true, FileReference = "upload-1", FileName = "nda.pdf" },
            InternalNote = "Company branch: Head office / สำนักงานใหญ่"
        });

        Assert.NotNull(result);
        Assert.Contains("POST /customer/v1/companies", calls);
        Assert.Contains("POST /customer/v1/customers", calls);
        Assert.Contains("POST /customer/v1/addresses", calls);
        Assert.Contains("POST /customer/v1/documents", calls);
        Assert.Contains("POST /customer/v1/ndas", calls);
        Assert.Contains("POST /customer/v1/internal-notes", calls);

        Assert.Contains(addressPayloads, payload =>
        {
            using var address = System.Text.Json.JsonDocument.Parse(payload);
            return address.RootElement.GetProperty("ownerType").GetString() == "Company"
                && address.RootElement.GetProperty("ownerId").GetGuid() == companyId;
        });

        using var document = System.Text.Json.JsonDocument.Parse(documentPayload);
        Assert.Equal("Signed", document.RootElement.GetProperty("documentSubType").GetString());

        using var ndaStatus = System.Text.Json.JsonDocument.Parse(ndaStatusPayload);
        Assert.Equal("Signed", ndaStatus.RootElement.GetProperty("status").GetString());
        Assert.Equal(42u, ndaStatus.RootElement.GetProperty("xmin").GetUInt32());
    }

    [Fact]
    public async Task CreateCustomerBasicAsync_WhenCustomerServiceReturnsValidationError_ThrowsUpstreamMessage()
    {
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.PathAndQuery == "/customer/v1/customers")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
                {
                    Content = JsonContent.Create(new ApiErrorResponse
                    {
                        Message = "Customer profile could not be created.",
                        Details = new Dictionary<string, string[]>
                        {
                            ["Email"] = ["A customer with email 'same@example.com' already exists"]
                        }
                    })
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var client = new CustomerServiceClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }, new Mock<ILogger<CustomerServiceClient>>().Object);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.CreateCustomerBasicAsync(new CustomerOnboardingRequest
        {
            Customer = new CreateCustomerRequest
            {
                FirstName = "Same",
                LastName = "Customer",
                Email = "same@example.com"
            }
        }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
        Assert.Contains("same@example.com", exception.Message);
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
        var accountManagerId = Guid.NewGuid();
        var customer = new CustomerDetailDto { Id = customerId, FirstName = "John", AccountManagerEmployeeId = accountManagerId };

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
        Assert.Equal(accountManagerId, result.AccountManagerEmployeeId);
    }

    [Fact]
    public async Task UpdateCustomerFullAsync_ForwardsXminAndAccountManagerEmployeeId()
    {
        var customerId = Guid.NewGuid();
        var accountManagerId = Guid.NewGuid();
        var capturedPayload = string.Empty;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(message =>
                    message.Method == HttpMethod.Get &&
                    message.RequestUri!.PathAndQuery == $"/customer/v1/customers/{customerId}"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new CustomerDetailDto { Id = customerId, Xmin = 123 })
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(message =>
                    message.Method == HttpMethod.Patch &&
                    message.RequestUri!.PathAndQuery == $"/customer/v1/customers/{customerId}"),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage message, CancellationToken _) =>
            {
                capturedPayload = await message.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new CustomerResponse { Id = customerId, AccountManagerEmployeeId = accountManagerId })
                };
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(message =>
                    message.Method == HttpMethod.Get &&
                    message.RequestUri!.PathAndQuery.Contains("/customer/v1/addresses?ownerType=Customer", StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<AddressResponse>())
            });

        var result = await _client.UpdateCustomerFullAsync(customerId, new CustomerOnboardingRequest
        {
            Customer = new CreateCustomerRequest
            {
                FirstName = "Sarah",
                LastName = "Chen",
                Email = "sarah@example.com",
                Segment = "Enterprise",
                Tier = "Gold",
                PreferredLanguage = "en",
                Timezone = "Asia/Bangkok",
                PaymentTerms = "Net 30",
                AccountManagerEmployeeId = accountManagerId
            }
        });

        Assert.NotNull(result);
        using var document = System.Text.Json.JsonDocument.Parse(capturedPayload);
        var root = document.RootElement;
        Assert.Equal(accountManagerId.ToString(), root.GetProperty("accountManagerEmployeeId").GetString());
        Assert.Equal("Net 30", root.GetProperty("paymentTerms").GetString());
        Assert.False(root.GetProperty("clearAccountManager").GetBoolean());
        Assert.Equal(123u, root.GetProperty("xmin").GetUInt32());
    }

    [Fact]
    public async Task GetPaymentTermsAsync_ReturnsReferenceData()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(message =>
                    message.Method == HttpMethod.Get &&
                    message.RequestUri!.PathAndQuery == "/customer/v1/customers/payment-terms"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<PaymentTermDto>
                {
                    new()
                    {
                        Code = "TWO_TEN_NET_30",
                        Name = "2/10 Net 30",
                        Category = "Discount",
                        Description = "Customer may deduct 2% if payment is received within 10 days; otherwise the full amount is due in 30 days.",
                        TypicalUse = "Use for approved accounts where faster cash collection is worth the discount.",
                        DueDays = 30,
                        DiscountPercent = 2m,
                        DiscountDays = 10,
                        SortOrder = 85
                    }
                })
            });

        var result = await _client.GetPaymentTermsAsync();

        var term = Assert.Single(result);
        Assert.Equal("2/10 Net 30", term.Name);
        Assert.Equal("Discount", term.Category);
        Assert.Equal(2m, term.DiscountPercent);
        Assert.Contains("faster cash collection", term.TypicalUse, StringComparison.Ordinal);
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

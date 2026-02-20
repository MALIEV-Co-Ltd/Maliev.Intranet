using System.Net;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Services;

public class ChatContextResolverTests
{
    private readonly Mock<ChatbotServiceClient> _chatbotClientMock;
    private readonly Mock<CustomerServiceClient> _customerClientMock;
    private readonly Mock<ILogger<ChatContextResolver>> _loggerMock;
    private readonly ChatContextResolver _resolver;

    public ChatContextResolverTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        _chatbotClientMock = new Mock<ChatbotServiceClient>(httpClient, new Mock<ILogger<ChatbotServiceClient>>().Object);
        _customerClientMock = new Mock<CustomerServiceClient>(httpClient, new Mock<ILogger<CustomerServiceClient>>().Object);
        _loggerMock = new Mock<ILogger<ChatContextResolver>>();

        _resolver = new ChatContextResolver(_chatbotClientMock.Object, _customerClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ResolveContextAsync_ShouldReturnNull_WhenNoIntent()
    {
        _chatbotClientMock.Setup(x => x.ExtractCustomerIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatbotCustomerIntentResponse?)null);

        var result = await _resolver.ResolveContextAsync("hi", null, Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveContextAsync_ShouldReturnContext_WhenCustomerFoundInUrl()
    {
        var customerId = Guid.NewGuid();
        var contextUrl = $"/sales/customers/{customerId}";

        _chatbotClientMock.Setup(x => x.ExtractCustomerIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotCustomerIntentResponse { NeedsCustomerData = true });

        _customerClientMock.Setup(x => x.GetCustomerByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDetailDto { Id = customerId, Name = "John Doe", Email = "john@test.com" });

        var result = await _resolver.ResolveContextAsync("tell me about this customer", contextUrl, Guid.NewGuid(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("John Doe", result);
        Assert.Contains("john@test.com", result);
    }

    [Fact]
    public async Task ResolveContextAsync_ShouldReturnSearchSummary_WhenMultipleCustomersFound()
    {
        _chatbotClientMock.Setup(x => x.ExtractCustomerIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatbotCustomerIntentResponse { NeedsCustomerData = true, CustomerSearchTerm = "John" });

        var customers = new List<CustomerSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "John Alpha", Email = "a@test.com" },
            new() { Id = Guid.NewGuid(), Name = "John Beta", Email = "b@test.com" }
        };
        _customerClientMock.Setup(x => x.GetCustomersAsync("John", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<CustomerSummaryDto> { Data = customers });

        var result = await _resolver.ResolveContextAsync("find John", null, Guid.NewGuid(), CancellationToken.None);

        Assert.Contains("Multiple customers found for 'John'", result);
        Assert.Contains("John Alpha", result);
    }
}

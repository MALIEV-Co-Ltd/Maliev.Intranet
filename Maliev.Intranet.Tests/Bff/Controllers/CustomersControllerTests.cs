using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class CustomersControllerTests
{
    private readonly Mock<CustomerServiceClient> _customerClientMock;
    private readonly Mock<RegistryServiceClient> _registryClientMock;
    private readonly Mock<IReferenceDataService> _refDataServiceMock;
    private readonly Mock<IAMServiceClient> _iamClientMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
    private readonly Mock<ILogger<CustomersController>> _loggerMock;
    private readonly NominatimGeocodingService _geocodingService;
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        var clientLogger = new Mock<ILogger<CustomerServiceClient>>().Object;
        _customerClientMock = new Mock<CustomerServiceClient>(httpClient, clientLogger);

        _registryClientMock = new Mock<RegistryServiceClient>(httpClient);
        _refDataServiceMock = new Mock<IReferenceDataService>();
        _iamClientMock = new Mock<IAMServiceClient>(httpClient);
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _hubContextMock = new Mock<IHubContext<NotificationHub>>();
        _loggerMock = new Mock<ILogger<CustomersController>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(factory => factory.CreateClient("Nominatim"))
            .Returns(new HttpClient(new MockHttpMessageHandler()) { BaseAddress = new Uri("https://nominatim.openstreetmap.org/") });
        _geocodingService = new NominatimGeocodingService(
            httpClientFactoryMock.Object,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<NominatimGeocodingService>>().Object);

        _controller = new CustomersController(
            _customerClientMock.Object,
            _registryClientMock.Object,
            _refDataServiceMock.Object,
            _iamClientMock.Object,
            _publishEndpointMock.Object,
            _hubContextMock.Object,
            _geocodingService,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenSuccessful()
    {
        var response = new PagedResponse<CustomerSummaryDto>();
        _customerClientMock.Setup(x => x.GetCustomersAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.Get(null, null, null, false, 1, 20, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenCustomerMissing()
    {
        _customerClientMock.Setup(x => x.GetCustomerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDetailDto?)null);

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetThaiLocationsMultiField_ForwardsRegistryWireShape()
    {
        var locations = new List<RegistryThaiLocation>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PostalCode = "11120",
                SubDistrictTh = "คลองข่อย",
                DistrictTh = "ปากเกร็ด",
                ProvinceTh = "นนทบุรี",
                SubDistrictEn = "Khlong Khoi",
                DistrictEn = "Pak Kret",
                ProvinceEn = "Nonthaburi"
            }
        };
        _registryClientMock.Setup(x => x.AutocompleteLocationsMultiFieldAsync(
                "11120",
                "คลองข่อย",
                "ปากเกร็ด",
                "นนทบุรี",
                8,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(locations);

        var result = await _controller.GetThaiLocationsMultiField("11120", "คลองข่อย", "ปากเกร็ด", "นนทบุรี", 8, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<List<RegistryThaiLocation>>(okResult.Value);
        var location = Assert.Single(value);
        Assert.Equal("คลองข่อย", location.SubDistrictTh);
        Assert.Equal("Khlong Khoi", location.SubDistrictEn);
    }

    [Fact]
    public async Task SendEmail_ShouldPublishNotification_WhenCustomerHasPrincipal()
    {
        var customerId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        _customerClientMock.Setup(x => x.GetCustomerByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDetailDto
            {
                Id = customerId,
                PrincipalId = principalId,
                Name = "Test Customer",
                Email = "customer@example.com",
                PreferredLanguage = "en"
            });

        var result = await _controller.SendEmail(customerId, new CustomerEmailRequest
        {
            Subject = "Quote update",
            Body = "Your quote is ready."
        }, CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        _publishEndpointMock.Verify(x => x.Publish(
            It.Is<Maliev.MessagingContracts.Contracts.Shared.NotificationEvent>(notification =>
                notification.Payload.TargetUsers.Single().UserId == principalId.ToString()
                && notification.Payload.NotificationType == "Quote update"
                && notification.Payload.TemplateId == string.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmail_ShouldReturnBadRequest_WhenCustomerHasNoPrincipal()
    {
        var customerId = Guid.NewGuid();
        _customerClientMock.Setup(x => x.GetCustomerByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerDetailDto { Id = customerId, Name = "Test Customer" });

        var result = await _controller.SendEmail(customerId, new CustomerEmailRequest
        {
            Subject = "Quote update",
            Body = "Your quote is ready."
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _publishEndpointMock.Verify(x => x.Publish(
            It.IsAny<Maliev.MessagingContracts.Contracts.Shared.NotificationEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBasic_ShouldReturnOk_WhenSuccessful()
    {
        var request = new CustomerOnboardingRequest { Customer = new CreateCustomerRequest() };
        var response = new CustomerResponse { Id = Guid.NewGuid() };

        _customerClientMock.Setup(x => x.CreateCustomerBasicAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Mock SignalR
        var clientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        _hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        clientsMock.Setup(x => x.All).Returns(clientProxyMock.Object);

        var result = await _controller.CreateBasic(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task CreateBasic_ShouldReturnBadRequest_WhenFailed()
    {
        var request = new CustomerOnboardingRequest { Customer = new CreateCustomerRequest() };
        _customerClientMock.Setup(x => x.CreateCustomerBasicAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerResponse?)null);

        var result = await _controller.CreateBasic(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}

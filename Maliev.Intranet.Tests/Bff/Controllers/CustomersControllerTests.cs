using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Intranet.Tests.Testing;

namespace Maliev.Intranet.Tests.Bff.Controllers;

/// <summary>Tests for the customers controller.</summary>
public class CustomersControllerTests
{
    private readonly Mock<CustomerServiceClient> _customerClientMock;
    private readonly Mock<RegistryServiceClient> _registryClientMock;
    private readonly Mock<IReferenceDataService> _refDataServiceMock;
    private readonly Mock<IAMServiceClient> _iamClientMock;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
    private readonly Mock<ILogger<CustomersController>> _loggerMock;
    private readonly CustomersController _controller;

    /// <summary>Initializes a new instance of the <see cref="CustomersControllerTests"/> class.</summary>
    public CustomersControllerTests()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler());
        var clientLogger = new Mock<ILogger<CustomerServiceClient>>().Object;
        _customerClientMock = new Mock<CustomerServiceClient>(httpClient, clientLogger);

        _registryClientMock = new Mock<RegistryServiceClient>(httpClient);
        _refDataServiceMock = new Mock<IReferenceDataService>();
        _iamClientMock = new Mock<IAMServiceClient>(httpClient);
        _hubContextMock = new Mock<IHubContext<NotificationHub>>();
        _loggerMock = new Mock<ILogger<CustomersController>>();

        _controller = new CustomersController(
            _customerClientMock.Object,
            _registryClientMock.Object,
            _refDataServiceMock.Object,
            _iamClientMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object);
    }

    /// <summary>Verifies that Get returns OK when the operation is successful.</summary>
    [Fact]
    public async Task Get_ShouldReturnOk_WhenSuccessful()
    {
        var response = new PagedResponse<CustomerSummaryDto>();
        _customerClientMock.Setup(x => x.GetCustomersAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync(response);

        var result = await _controller.Get(null, null, 1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    /// <summary>Verifies that GetById returns NotFound when the customer does not exist.</summary>
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenCustomerMissing()
    {
        _customerClientMock.Setup(x => x.GetCustomerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDetailDto?)null);

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>Verifies that CreateBasic returns OK when the customer is created successfully.</summary>
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

    /// <summary>Verifies that CreateBasic returns BadRequest when the creation fails.</summary>
    [Fact]
    public async Task CreateBasic_ShouldReturnBadRequest_WhenFailed()
    {
        var request = new CustomerOnboardingRequest { Customer = new CreateCustomerRequest() };
        _customerClientMock.Setup(x => x.CreateCustomerBasicAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerResponse?)null);

        var result = await _controller.CreateBasic(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>Verifies that GetIdentity returns OK with correct mapping.</summary>
    [Fact]
    public async Task GetIdentity_ShouldReturnOk_WhenSuccessful()
    {
        var customerId = Guid.NewGuid();
        var customer = new CustomerDetailDto 
        { 
            Id = customerId, 
            FirstName = "John", 
            LastName = "Doe",
            CompanyName = "Acme Corp",
            CompanyVatNumber = "123456789"
        };
        _customerClientMock.Setup(x => x.GetCustomerByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _controller.GetIdentity(customerId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var identity = Assert.IsType<CustomerIdentityDto>(okResult.Value);
        Assert.Equal(customer.Id, identity.Id);
        Assert.Equal(customer.FirstName, identity.FirstName);
        Assert.Equal(customer.LastName, identity.LastName);
        Assert.Equal(customer.CompanyName, identity.CompanyName);
        Assert.Equal(customer.CompanyVatNumber, identity.CompanyTaxId);
    }
}

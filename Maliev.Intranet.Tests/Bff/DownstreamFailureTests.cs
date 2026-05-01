using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Maliev.Intranet.Tests.Bff;

public class DownstreamFailureTests : IClassFixture<BffTestWebApplicationFactory>
{
    private readonly BffTestWebApplicationFactory _factory;

    public DownstreamFailureTests(BffTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Requires Docker/RabbitMQ — host startup hangs due to service discovery and IAM token provider")]
    public async Task GetTimeOffBalances_WhenLeaveServiceFails_ReturnsError()
    {
        // Arrange
        var mockClient = new Mock<ILeaveServiceClient>();
        mockClient.Setup(x => x.GetMyBalancesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service Unavailable"));


        var mockEmployeeClient = new Mock<EmployeeServiceClient>(new HttpClient { BaseAddress = new Uri("http://localhost") });
        mockEmployeeClient.Setup(x => x.GetByPrincipalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeDetailDto { Id = Guid.NewGuid() });

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockClient.Object);
                services.AddScoped(_ => mockEmployeeClient.Object);
            });
        }).CreateClient();

        var principalId = Guid.NewGuid().ToString();
        var token = _factory.CreateTestToken(principalId, MalievPermissions.Leave.Read);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/v1/timeoff/balances");

        // Assert
        // We expect an error status code. 500 is preferred, but 404 from exception handler 
        // also indicates a failure was caught and redirected.
        Assert.True((int)response.StatusCode >= 500, $"Expected 5xx status but got {response.StatusCode}");
    }

    [Fact(Skip = "Requires Docker/RabbitMQ — host startup hangs due to service discovery and IAM token provider")]
    public async Task GetCompanies_WhenCustomerServiceReturnsError_ReturnsError()
    {
        // Arrange
        var mockClient = new Mock<CustomerServiceClient>(new HttpClient(), null!);
        mockClient.Setup(x => x.GetCompaniesAsync(null, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Downstream error"));


        var mockEmployeeClient = new Mock<EmployeeServiceClient>(new HttpClient { BaseAddress = new Uri("http://localhost") });
        mockEmployeeClient.Setup(x => x.GetByPrincipalIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeDetailDto { Id = Guid.NewGuid() });

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockClient.Object);
                services.AddScoped(_ => mockEmployeeClient.Object);
            });
        }).CreateClient();

        var token = _factory.CreateTestToken("test-user", MalievPermissions.Customer.Read);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/v1/companies");

        // Assert
        Assert.True((int)response.StatusCode >= 500, $"Expected 5xx status but got {response.StatusCode}");
    }
}

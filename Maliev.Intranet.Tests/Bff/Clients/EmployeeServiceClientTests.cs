using System.Net;
using System.Net.Http.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Shared;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Clients;

public class EmployeeServiceClientTests
{
    [Fact]
    public async Task GetEmployeesAsync_UsesEmployeeServicePagedContract()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri!.PathAndQuery == "/employee/v1/employees?page=2&pageSize=100"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    data = new[]
                    {
                        new
                        {
                            id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                            name = "Mia Wong",
                            email = "mia.wong@maliev.com",
                            department = "Sales",
                            title = "Sales Manager",
                            status = "Active"
                        }
                    },
                    meta = new
                    {
                        currentPage = 2,
                        totalPages = 4,
                        totalItems = 301,
                        totalCount = 301,
                        pageSize = 100
                    }
                })
            });

        var client = new EmployeeServiceClient(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://employee")
        });

        var result = await client.GetEmployeesAsync(2, 100);

        Assert.NotNull(result);
        var employee = Assert.Single(result.Data);
        Assert.Equal(Guid.Parse("55555555-5555-5555-5555-555555555555"), employee.Id);
        Assert.Equal("Mia Wong", employee.Name);
        Assert.Equal("Sales Manager", employee.Title);
        Assert.Equal(2, result.Meta.CurrentPage);
        Assert.Equal(100, result.Meta.PageSize);
        Assert.Equal(301, result.Meta.TotalCount);
    }

    [Fact]
    public async Task GetSelfServiceProfileAsync_UsesEmployeeServiceProfileContract()
    {
        var employeeId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Get &&
                    request.RequestUri!.PathAndQuery == $"/employee/v1/profile/{employeeId}/profile"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = employeeId,
                    employeeNumber = "EMP-001",
                    firstName = "Mia",
                    lastName = "Wong",
                    fullName = "Mia Wong",
                    preferredName = "Mia",
                    workEmail = "mia.wong@maliev.com",
                    personalEmail = "mia.personal@example.com",
                    mobilePhone = "+66810000000",
                    employmentType = "FullTime",
                    employmentStatus = "Active"
                })
            });

        var client = new EmployeeServiceClient(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://employee")
        });

        var result = await client.GetSelfServiceProfileAsync(employeeId);

        Assert.NotNull(result);
        Assert.Equal(employeeId, result.Id);
        Assert.Equal("EMP-001", result.EmployeeNumber);
        Assert.Equal("mia.personal@example.com", result.PersonalEmail);
        Assert.Equal("+66810000000", result.MobilePhone);
    }

    [Fact]
    public async Task UpdateSelfServiceProfileAsync_UsesEmployeeServiceProfileContract()
    {
        var employeeId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        string? json = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Put &&
                    request.RequestUri!.PathAndQuery == $"/employee/v1/profile/{employeeId}/profile"),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                json = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var client = new EmployeeServiceClient(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://employee")
        });

        var result = await client.UpdateSelfServiceProfileAsync(employeeId, new UpdateEmployeeSelfProfileRequest
        {
            PreferredName = "M",
            PersonalEmail = "mia.personal@example.com",
            MobilePhone = "+66810000000"
        });

        Assert.True(result);
        Assert.Contains("\"preferredName\":\"M\"", json, StringComparison.Ordinal);
        Assert.Contains("\"personalEmail\":\"mia.personal@example.com\"", json, StringComparison.Ordinal);
        Assert.Contains("\"mobilePhone\":\"\\u002B66810000000\"", json, StringComparison.Ordinal);
    }
}

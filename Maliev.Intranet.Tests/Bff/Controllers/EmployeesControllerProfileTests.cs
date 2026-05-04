using System.Security.Claims;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Controllers;
using Maliev.Intranet.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public sealed class EmployeesControllerProfileTests
{
    [Fact]
    public async Task GetMe_FillsGoogleEmailDefaultsAndPlatformOwnerRole()
    {
        var principalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var createdAt = new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc);
        var employeeClient = new Mock<EmployeeServiceClient>(new HttpClient { BaseAddress = new Uri("http://employee") });
        employeeClient.Setup(client => client.GetByPrincipalIdAsync(principalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeDetailDto
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FirstName = "Natthapol",
                LastName = "Vanasrivilai",
                Email = "",
                Status = "",
                EmployeeType = "",
                CreatedAt = createdAt
            });

        var iamClient = new Mock<IAMServiceClient>(new HttpClient { BaseAddress = new Uri("http://iam") });
        iamClient.Setup(client => client.GetPrincipalRolesAsync(principalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RoleBindingDto
                {
                    BindingId = Guid.NewGuid().ToString(),
                    PrincipalId = principalId,
                    RoleId = "roles.platform.owner",
                    RoleName = ""
                }
            ]);

        var controller = new EmployeesController(employeeClient.Object, iamClient.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim("sub", principalId.ToString()),
                        new Claim("email", "test@test.com")
                    ], "Test"))
                }
            }
        };

        var result = await controller.GetMe(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<EmployeeDetailDto>(ok.Value);

        Assert.Equal("test@test.com", profile.Email);
        Assert.Equal("Active", profile.Status);
        Assert.Equal("FullTime", profile.EmployeeType);
        Assert.Equal(createdAt, profile.HireDate);
        Assert.Equal("Platform Owner", profile.Role);
        Assert.Equal("Platform Owner", profile.Title);
    }

    [Fact]
    public async Task GetMe_UsesIamPrincipalCreatedAtWhenEmployeeDatesAreMissing()
    {
        var principalId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var principalCreatedAt = new DateTime(2026, 5, 4, 9, 30, 0, DateTimeKind.Utc);
        var employeeClient = new Mock<EmployeeServiceClient>(new HttpClient { BaseAddress = new Uri("http://employee") });
        employeeClient.Setup(client => client.GetByPrincipalIdAsync(principalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmployeeDetailDto
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                FirstName = "Natthapol",
                LastName = "Vanasrivilai"
            });

        var iamClient = new Mock<IAMServiceClient>(new HttpClient { BaseAddress = new Uri("http://iam") });
        iamClient.Setup(client => client.GetPrincipalRolesAsync(principalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        iamClient.Setup(client => client.GetPrincipalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PrincipalSummaryDto
                {
                    Id = principalId,
                    PrincipalId = principalId,
                    Email = "test@test.com",
                    DisplayName = "Natthapol Vanasrivilai",
                    CreatedAt = principalCreatedAt
                }
            ]);

        var controller = new EmployeesController(employeeClient.Object, iamClient.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim("sub", principalId.ToString()),
                        new Claim("email", "test@test.com")
                    ], "Test"))
                }
            }
        };

        var result = await controller.GetMe(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<EmployeeDetailDto>(ok.Value);

        Assert.Equal(principalCreatedAt, profile.HireDate);
        Assert.Equal("Employee", profile.Role);
    }
}

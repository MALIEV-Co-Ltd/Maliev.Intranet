using System.Security.Claims;
using Maliev.Intranet.Bff.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maliev.Intranet.Tests.Bff.Middleware;

public class WorkspaceEmailDomainEnforcementMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AuthenticatedApiRequestWithExternalEmail_ReturnsForbiddenAndDoesNotContinue()
    {
        var nextCalled = false;
        var middleware = new WorkspaceEmailDomainEnforcementMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<WorkspaceEmailDomainEnforcementMiddleware>.Instance);
        var httpContext = CreateContext("/api/v1/auth/user", "contractor@example.com");
        var authServiceMock = AddAuthenticationService(httpContext);

        await middleware.InvokeAsync(httpContext);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        authServiceMock.Verify(
            x => x.SignOutAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedPageRequestWithExternalEmail_RedirectsToLoginAndDoesNotContinue()
    {
        var nextCalled = false;
        var middleware = new WorkspaceEmailDomainEnforcementMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<WorkspaceEmailDomainEnforcementMiddleware>.Instance);
        var httpContext = CreateContext("/orders", "contractor@example.com");
        AddAuthenticationService(httpContext);

        await middleware.InvokeAsync(httpContext);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        Assert.StartsWith("/login?error=", httpContext.Response.Headers.Location.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedRequestWithMalievEmail_Continues()
    {
        var nextCalled = false;
        var middleware = new WorkspaceEmailDomainEnforcementMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<WorkspaceEmailDomainEnforcementMiddleware>.Instance);
        var httpContext = CreateContext("/orders", "employee@maliev.com");
        AddAuthenticationService(httpContext);

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedServiceAccountWithoutEmail_Continues()
    {
        var nextCalled = false;
        var middleware = new WorkspaceEmailDomainEnforcementMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<WorkspaceEmailDomainEnforcementMiddleware>.Instance);
        var httpContext = CreateServiceAccountContext("/api/v1/seed/customers");
        AddAuthenticationService(httpContext);

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext(string path, string email)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Email, email)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        context.Request.Path = path;
        return context;
    }

    private static DefaultHttpContext CreateServiceAccountContext(string path)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "system:service:intranetbff"),
                new Claim("service_name", "IntranetBff"),
                new Claim("user_type", "service"),
                new Claim("role", "service-account")
            ],
            "Bearer");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        context.Request.Path = path;
        return context;
    }

    private static Mock<IAuthenticationService> AddAuthenticationService(DefaultHttpContext httpContext)
    {
        var authServiceMock = new Mock<IAuthenticationService>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);
        httpContext.RequestServices = serviceProviderMock.Object;
        return authServiceMock;
    }
}

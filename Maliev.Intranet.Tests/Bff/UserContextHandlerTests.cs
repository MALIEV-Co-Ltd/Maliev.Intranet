using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Maliev.Intranet.Bff;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.Intranet.Tests.Bff;

public class UserContextHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<UserContextHandler>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;
    private readonly UserContextHandler _handler;

    public UserContextHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<UserContextHandler>>();
        _innerHandlerMock = new Mock<HttpMessageHandler>();

        _handler = new UserContextHandler(_httpContextAccessorMock.Object, _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };
    }

    [Fact]
    public async Task SendAsync_ShouldAddHeaders_WhenUserIsAuthenticated()
    {
        var userId = "test-user-123";
        var accessToken = "test-token";
        var claims = new List<Claim>
        {
            new("user_id", userId),
            new("access_token", accessToken)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var services = new ServiceCollection();
        var authServiceMock = new Mock<IAuthenticationService>();
        services.AddSingleton(authServiceMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.User = principal;
        httpContext.RequestServices = serviceProvider;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://downstream/api");

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var invoker = new HttpMessageInvoker(_handler);
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.True(request.Headers.Contains("X-User-Id"));
        Assert.Equal(userId, request.Headers.GetValues("X-User-Id").First());
        Assert.Equal($"Bearer {accessToken}", request.Headers.Authorization?.ToString());
    }

    [Fact]
    public async Task SendAsync_ShouldUseSubClaim_WhenUserIdMissing()
    {
        var sub = "sub-123";
        var claims = new List<Claim> { new("sub", sub) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IAuthenticationService>().Object);
        httpContext.RequestServices = services.BuildServiceProvider();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://downstream/api");
        _innerHandlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        await new HttpMessageInvoker(_handler).SendAsync(request, CancellationToken.None);

        Assert.Equal(sub, request.Headers.GetValues("X-User-Id").First());
    }

    [Fact]
    public async Task SendAsync_ShouldNotAddHeaders_WhenUserIsNotAuthenticated()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://downstream/api");

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var invoker = new HttpMessageInvoker(_handler);
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.False(request.Headers.Contains("X-User-Id"));
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_ShouldNotAddHeaders_WhenHttpContextIsNull()
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://downstream/api");

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var invoker = new HttpMessageInvoker(_handler);
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.False(request.Headers.Contains("X-User-Id"));
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnServiceUnavailable_OnException()
    {
        var services = new ServiceCollection();
        var authServiceMock = new Mock<IAuthenticationService>();
        services.AddSingleton(authServiceMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user") }, "Test"));
        httpContext.RequestServices = serviceProvider;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://downstream/api");

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Network error"));

        var invoker = new HttpMessageInvoker(_handler);
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}

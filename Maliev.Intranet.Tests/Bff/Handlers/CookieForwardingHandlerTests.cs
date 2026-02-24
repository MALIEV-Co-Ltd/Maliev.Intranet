using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using Maliev.Intranet.Bff.Handlers;
using System.Net;

namespace Maliev.Intranet.Tests.Bff.Handlers;

public class CookieForwardingHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;
    private readonly CookieForwardingHandler _handler;

    public CookieForwardingHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _innerHandlerMock = new Mock<HttpMessageHandler>();
        _handler = new CookieForwardingHandler(_httpContextAccessorMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };
    }

    [Fact]
    public async Task SendAsync_ShouldForwardCookiesAndAuth_WhenHttpContextExists()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "test-cookie=value";
        httpContext.Request.Headers.Authorization = "Bearer test-token";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://internal/api");

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var invoker = new HttpMessageInvoker(_handler);
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal("test-cookie=value", request.Headers.GetValues("Cookie").First());
        Assert.Equal("Bearer test-token", request.Headers.Authorization?.ToString());
    }

    [Fact]
    public async Task SendAsync_ShouldDoNothing_WhenHttpContextIsNull()
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://internal/api");

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var invoker = new HttpMessageInvoker(_handler);
        await invoker.SendAsync(request, CancellationToken.None);

        Assert.Empty(request.Headers);
    }
}

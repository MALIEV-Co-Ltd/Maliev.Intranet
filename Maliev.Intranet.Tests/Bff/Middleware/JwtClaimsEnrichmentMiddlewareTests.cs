using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Maliev.Intranet.Bff.Middleware;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Maliev.Intranet.Tests.Bff.Middleware;

public class JwtClaimsEnrichmentMiddlewareTests
{
    private readonly Mock<ILogger<JwtClaimsEnrichmentMiddleware>> _loggerMock;
    private readonly JwtClaimsEnrichmentMiddleware _middleware;
    private bool _nextCalled;

    public JwtClaimsEnrichmentMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<JwtClaimsEnrichmentMiddleware>>();
        _middleware = new JwtClaimsEnrichmentMiddleware(context =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        }, _loggerMock.Object);
    }

    private string CreateToken(params (string Type, string Value)[] claims)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-at-least-32-chars-long"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claimObjects = claims.Select(c => new Claim(c.Type, c.Value)).ToList();

        var token = new JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: claimObjects,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return handler.WriteToken(token);
    }

    [Fact]
    public async Task InvokeAsync_ShouldEnrichClaims_WhenAuthenticatedWithToken()
    {
        var token = CreateToken(("roles", "Admin"), ("permissions", "read:all"));

        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[] { new Claim("access_token", token) }, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        var authServiceMock = new Mock<IAuthenticationService>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);
        httpContext.RequestServices = serviceProviderMock.Object;

        await _middleware.InvokeAsync(httpContext);

        Assert.True(_nextCalled);
        Assert.True(identity.HasClaim(ClaimTypes.Role, "Admin"));
        Assert.True(identity.HasClaim("permissions", "read:all"));
    }

    [Fact]
    public async Task InvokeAsync_ShouldSignOut_WhenTokenIsMissing()
    {
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new Claim[] { }, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        var authServiceMock = new Mock<IAuthenticationService>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);
        httpContext.RequestServices = serviceProviderMock.Object;

        await _middleware.InvokeAsync(httpContext);

        authServiceMock.Verify(x => x.SignOutAsync(httpContext, It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenNotAuthenticated()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        await _middleware.InvokeAsync(httpContext);

        Assert.True(_nextCalled);
    }
}

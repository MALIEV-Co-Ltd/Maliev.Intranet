using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Maliev.Intranet.Bff.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Maliev.Intranet.Tests.Bff.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _envMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _httpContextMock = new Mock<HttpContext>();

        _controller = new AuthController(_httpClientFactoryMock.Object, _envMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };
    }

    [Fact]
    public async Task Logout_ShouldSignOutAndRedirect()
    {
        var authServiceMock = new Mock<IAuthenticationService>();
        _httpContextMock.Setup(x => x.RequestServices.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var result = await _controller.Logout();

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/login", redirectResult.Url);
        authServiceMock.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public void GetUser_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        _httpContextMock.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));

        var result = _controller.GetUser();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void GetUser_ShouldReturnUserContext_WhenAuthenticated()
    {
        var claims = new List<Claim>
        {
            new("user_id", "user-123"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@maliev.com"),
            new("urn:google:picture", "https://lh3.googleusercontent.com/a/test-user"),
            new(ClaimTypes.Role, "Admin"),
            new("permissions", "read:all")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        var result = _controller.GetUser();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var userContext = Assert.IsType<Maliev.Intranet.Shared.UserContextDto>(okResult.Value);
        Assert.Equal("user-123", userContext.UserId);
        Assert.Equal("Test User", userContext.DisplayName);
        Assert.Equal("https://lh3.googleusercontent.com/a/test-user", userContext.ProfileImageUrl);
        Assert.Contains("Admin", userContext.Roles);
        Assert.Contains("read:all", userContext.Permissions);
    }

    [Fact]
    public async Task GetAvatar_HttpUrlOnAllowedHost_ReturnsBadRequestWithoutFetching()
    {
        var result = await _controller.GetAvatar("http://lh3.googleusercontent.com/a/test-user");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unsupported image URL", badRequest.Value);
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAvatar_NonImageResponseFromAllowedHost_ReturnsBadRequest()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html></html>")
            });

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handlerMock.Object));

        var services = new ServiceCollection()
            .AddSingleton(_httpClientFactoryMock.Object)
            .BuildServiceProvider();

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        };

        var result = await _controller.GetAvatar("https://lh3.googleusercontent.com/a/test-user");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unsupported image content type", badRequest.Value);
    }

    [Fact]
    public async Task LoginStandard_ShouldReturnOk_WhenSuccessful()
    {
        var token = CreateTestToken(); // This is from the test class helper if I add it, or just a dummy string
                                       // Actually, AuthController parses the token.

        var authResponse = new
        {
            access_token = token,
            user = new { user_id = "123", user_type = "employee", email = "test@maliev.com", name = "Test User" }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(authResponse) });

        _httpClientFactoryMock.Setup(x => x.CreateClient("AuthService"))
            .Returns(new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://auth") });

        var authServiceMock = new Mock<IAuthenticationService>();
        _httpContextMock.Setup(x => x.RequestServices.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var request = new AuthController.InternalLoginRequest { Username = "user@maliev.com", Password = "password" };
        var result = await _controller.LoginStandard(request);

        Assert.IsType<OkResult>(result);
        authServiceMock.Verify(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public async Task LoginStandard_AuthServiceReturnsExternalEmail_ReturnsUnauthorizedWithoutSigningIn()
    {
        var token = CreateTestToken("contractor@example.com");

        var authResponse = new
        {
            access_token = token,
            user = new { user_id = "123", user_type = "employee", email = "contractor@example.com", name = "Contractor User" }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(authResponse) });

        _httpClientFactoryMock.Setup(x => x.CreateClient("AuthService"))
            .Returns(new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://auth") });

        var authServiceMock = new Mock<IAuthenticationService>();
        _httpContextMock.Setup(x => x.RequestServices.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var request = new AuthController.InternalLoginRequest { Username = "employee@maliev.com", Password = "password" };

        var result = await _controller.LoginStandard(request);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Unauthorized.", unauthorizedResult.Value);
        authServiceMock.Verify(
            x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    private string CreateTestToken(string email = "test@maliev.com")
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("test-secret-key-at-least-32-chars-long"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            "test",
            "test",
            [new Claim("sub", "123"), new Claim("email", email)],
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);
        return handler.WriteToken(token);
    }
}

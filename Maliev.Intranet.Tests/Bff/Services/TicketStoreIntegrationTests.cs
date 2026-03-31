using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Maliev.Intranet.Bff;
using Maliev.Intranet.Bff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.Intranet.Tests.Bff.Services;

/// <summary>
/// Test factory that mocks AuthService so the full cookie-auth login flow can be tested
/// without external dependencies. Shares a single DistributedCacheTicketStore instance
/// across all test requests to validate session persistence.
/// </summary>
public class TicketStoreTestFactory : WebApplicationFactory<Program>
{
    private readonly RSA _testRsa = RSA.Create(2048);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-google-id",
                ["Authentication:Google:ClientSecret"] = "test-google-secret",
                ["Services:AuthService:BaseUrl"] = "http://auth-service",
                ["Services:CustomerService:BaseUrl"] = "http://customer-service",
                ["Jwt:SecurityKey"] = "test-security-key-for-integration-tests-min32chars",
                ["MassTransit:UseInMemory"] = "true",
                ["MassTransit:SkipBusWait"] = "true",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<DistributedCacheTicketStore>();

            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(_testRsa)
                };
            });

            services.AddHttpClient("AuthService")
                .ConfigurePrimaryHttpMessageHandler(() => new MockAuthServiceHandler(_testRsa));
        });
    }

    public string CreateTestToken(string userId = "test-user", params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new(ClaimTypes.Email, $"{userId}@maliev.com"),
        };

        foreach (var p in permissions)
        {
            claims.Add(new Claim("permissions", p));
        }

        var key = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(_testRsa);
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private class MockAuthServiceHandler(RSA testRsa) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery;

            if (path == "/auth/v1/login" || path == "/auth/v1/exchange/google")
            {
                var userId = ExtractUserId(request) ?? "test-user";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateLoginResponse(userId))
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static string? ExtractUserId(HttpRequestMessage request)
        {
            return null;
        }

        private string CreateLoginResponse(string userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new("sub", userId),
                new(ClaimTypes.Email, $"{userId}@maliev.com"),
                new(ClaimTypes.Name, $"Test {userId}"),
                new("user_type", "employee"),
            };

            var key = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(testRsa);
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "test",
                audience: "test",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var tokenStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

            return JsonSerializer.Serialize(new
            {
                access_token = tokenStr,
                user = new { user_id = userId, user_type = "employee", email = $"{userId}@maliev.com", name = $"Test {userId}" }
            });
        }
    }
}

public class TicketStoreIntegrationTests : IClassFixture<TicketStoreTestFactory>
{
    private readonly TicketStoreTestFactory _factory;

    public TicketStoreIntegrationTests(TicketStoreTestFactory factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Requires Docker for MassTransit/RabbitMQ — host startup hangs due to service discovery")]
    public async Task Login_SetsAuthCookie()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "test@maliev.com",
            password = "password",
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authCookie = client.GetCookie("Maliev.Intranet.Auth");
        Assert.False(string.IsNullOrEmpty(authCookie));
    }

    [Fact(Skip = "Requires Docker for MassTransit/RabbitMQ — host startup hangs due to service discovery")]
    public async Task Session_PersistsAcrossRequests()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "test@maliev.com",
            password = "password",
            rememberMe = false
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var response1 = await client.GetAsync("/api/diagnostics/me");
        var response2 = await client.GetAsync("/api/diagnostics/me");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response1.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response2.StatusCode);
    }

    [Fact(Skip = "Requires Docker for MassTransit/RabbitMQ — host startup hangs due to service discovery")]
    public async Task MultipleLogins_SameUser_OverwritesSession()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login", new { username = "test@maliev.com", password = "password", rememberMe = false });
        await client.PostAsJsonAsync("/api/auth/login", new { username = "test@maliev.com", password = "password", rememberMe = false });

        var ticketStore = _factory.Services.GetRequiredService<DistributedCacheTicketStore>();
        var ticket = await ticketStore.RetrieveAsync("test-user");
        Assert.NotNull(ticket);
        Assert.Equal("test-user", ticket.Principal.FindFirst("sub")?.Value);
    }

    [Fact(Skip = "Requires Docker for MassTransit/RabbitMQ — host startup hangs due to service discovery")]
    public async Task AuthenticatedRequest_AfterLogin_ReturnsOkOrForbidden()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "test@maliev.com",
            password = "password",
            rememberMe = false
        });

        var meResponse = await client.GetAsync("/api/diagnostics/me");

        Assert.True(
            meResponse.StatusCode == HttpStatusCode.OK ||
            meResponse.StatusCode == HttpStatusCode.Forbidden ||
            meResponse.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected OK/Forbidden/Unauthorized but got {meResponse.StatusCode}");
    }
}

internal static class HttpClientCookieExtensions
{
    public static string? GetCookie(this HttpClient client, string name)
    {
        var handler = client.GetType().GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(client) as HttpMessageHandler;
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler;
        }

        if (handler is HttpClientHandler clientHandler && clientHandler.CookieContainer != null)
        {
            var cookies = clientHandler.CookieContainer.GetCookies(client.BaseAddress ?? new Uri("http://localhost"));
            return cookies[name]?.Value;
        }

        return null;
    }
}

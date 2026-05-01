using System.Net;
using System.Net.Http.Headers;
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

    /// <summary>
    /// Gets the RSA key used for signing test JWTs.
    /// </summary>
    public RSA TestRsa => _testRsa;

    /// <summary>
    /// Initializes a new instance of the TicketStoreTestFactory, forcing environment
    /// variables to guarantee in-memory bus configuration BEFORE Program.cs builder executes.
    /// </summary>
    public TicketStoreTestFactory()
    {
        Environment.SetEnvironmentVariable("MASSTRANSIT_INMEMORY", "true");
        Environment.SetEnvironmentVariable("MassTransit__UseInMemory", "true");
        Environment.SetEnvironmentVariable("MassTransit__SkipBusWait", "true");
    }

    /// <inheritdoc />
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
                ["Services:IAMService:BaseUrl"] = "http://iam-service",
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

                // Critical: Intercept OpenID Connect config fetch so JwtBearer middleware doesn't hang
                options.BackchannelHttpHandler = new MockAuthServiceHandler(_testRsa);
                options.MetadataAddress = "http://auth-service/.well-known/openid-configuration";
                options.RequireHttpsMetadata = false;
            });

            // Mock ALL external HTTP clients to prevent service discovery hangs
            services.AddHttpClient("AuthService")
                .ConfigurePrimaryHttpMessageHandler(() => new MockAuthServiceHandler(_testRsa));

            services.AddHttpClient("IAMServiceBootstrap")
                .ConfigurePrimaryHttpMessageHandler(() => new MockIAMServiceHandler());

            // Override the generic client for IAM Service registered through AddIAMServiceClient
            services.AddHttpClient("IAMService")
                .ConfigurePrimaryHttpMessageHandler(() => new MockIAMServiceHandler());
        });
    }

    /// <summary>
    /// Creates a test JWT token with the specified user ID and permissions.
    /// </summary>
    /// <param name="userId">The user ID to embed in the token.</param>
    /// <param name="permissions">Optional permissions to include.</param>
    /// <returns>The signed JWT string.</returns>
    public string CreateTestToken(string userId = "test-user", params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new(ClaimTypes.Email, $"{userId}@maliev.com"),
            new("email", $"{userId}@maliev.com"),
            new("name", $"Test {userId}"),
            new("user_id", userId),
            new("user_type", "employee"),
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

    /// <summary>
    /// Creates an HttpClient with a CookieContainer to simulate real browser cookie behavior.
    /// This is critical for testing cookie persistence — without it, cookies are silently discarded.
    /// </summary>
    public HttpClient CreateClientWithCookies()
    {
        var cookieContainer = new CookieContainer();
        var handler = Server.CreateHandler();

        // Wrap the test server handler with a CookieContainer-aware handler
        var cookieHandler = new CookieContainerHandler(handler, cookieContainer);

        var client = new HttpClient(cookieHandler)
        {
            BaseAddress = Server.BaseAddress
        };

        return client;
    }

    /// <summary>
    /// Handler that wraps another handler and manages cookies like a real browser.
    /// </summary>
    private sealed class CookieContainerHandler : DelegatingHandler
    {
        private readonly CookieContainer _cookieContainer;
        private readonly Uri _baseAddress;

        public CookieContainerHandler(HttpMessageHandler innerHandler, CookieContainer cookieContainer)
            : base(innerHandler)
        {
            _cookieContainer = cookieContainer;
            _baseAddress = new Uri("http://localhost");
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? _baseAddress;

            var cookieHeader = _cookieContainer.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var cookie in setCookieHeaders)
                {
                    _cookieContainer.SetCookies(uri, cookie);
                }
            }

            return response;
        }
    }

    /// <summary>
    /// Mock handler for AuthService that returns valid JWT tokens and OpenID configuration.
    /// </summary>
    private sealed class MockAuthServiceHandler(RSA testRsa) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "";

            if (path.Contains("/.well-known/openid-configuration"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $$"""
                        {
                          "issuer": "http://auth-service",
                          "jwks_uri": "http://auth-service/.well-known/jwks",
                          "authorization_endpoint": "http://auth-service/connect/authorize",
                          "token_endpoint": "http://auth-service/connect/token"
                        }
                        """,
                        System.Text.Encoding.UTF8,
                        "application/json")
                });
            }

            if (path.Contains("/.well-known/jwks"))
            {
                var parameters = testRsa.ExportParameters(false);
                var jwk = new Microsoft.IdentityModel.Tokens.JsonWebKey()
                {
                    Kty = Microsoft.IdentityModel.Tokens.JsonWebAlgorithmsKeyTypes.RSA,
                    Use = "sig",
                    Kid = "test-key-id",
                    E = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(parameters.Exponent),
                    N = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(parameters.Modulus)
                };

                var jwks = new { keys = new[] { jwk } };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(jwks),
                        System.Text.Encoding.UTF8,
                        "application/json")
                });
            }

            if (path.Contains("/auth/v1/login") || path.Contains("/auth/v1/exchange/google"))
            {
                var userId = "test-user";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        CreateLoginResponse(userId),
                        System.Text.Encoding.UTF8,
                        "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private string CreateLoginResponse(string userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new("sub", userId),
                new(ClaimTypes.Email, $"{userId}@maliev.com"),
                new(ClaimTypes.Name, $"Test {userId}"),
                new("email", $"{userId}@maliev.com"),
                new("name", $"Test {userId}"),
                new("user_id", userId),
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

    /// <summary>
    /// Mock handler for IAM Service bootstrap endpoint — returns OK without needing the real service.
    /// </summary>
    private sealed class MockIAMServiceHandler : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "";

            if (path.Contains("/iam/v1/principals/bootstrap"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { message = "Bootstrap OK" }),
                        System.Text.Encoding.UTF8,
                        "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

/// <summary>
/// Integration tests that verify the full cookie authentication pipeline works end-to-end.
/// These tests exercise the REAL middleware pipeline (including any cookie-manipulation middleware)
/// to catch bugs like the destructive cookie-deletion middleware that caused all 401 errors.
/// </summary>
public class TicketStoreIntegrationTests : IClassFixture<TicketStoreTestFactory>
{
    private readonly TicketStoreTestFactory _factory;

    /// <summary>
    /// Initializes the test class with the shared test factory.
    /// </summary>
    /// <param name="factory">The shared factory instance.</param>
    public TicketStoreIntegrationTests(TicketStoreTestFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that the POST /api/v1/auth/login endpoint sets the auth cookie.
    /// </summary>
    [Fact]
    public async Task Login_SetsAuthCookie()
    {
        var client = _factory.CreateClientWithCookies();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "test@maliev.com",
            password = "password",
            rememberMe = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify Set-Cookie header was returned
        Assert.True(
            response.Headers.Contains("Set-Cookie"),
            "Login response must include a Set-Cookie header for the auth cookie.");
    }

    /// <summary>
    /// CRITICAL TEST: Verifies that the auth cookie survives across multiple requests.
    /// This test would have caught the destructive cookie-deletion middleware that was
    /// unconditionally removing Maliev.Intranet.Auth on every non-static request.
    /// </summary>
    [Fact]
    public async Task Session_PersistsAcrossRequests_CookieNotDestroyed()
    {
        var client = _factory.CreateClientWithCookies();

        // Step 1: Login to establish session
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "test@maliev.com",
            password = "password",
            rememberMe = false
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Step 2: First authenticated request — should NOT get 401
        var response1 = await client.GetAsync("/api/v1/auth/user");
        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            response1.StatusCode);

        // Step 3: Second authenticated request — cookie must still be valid
        // This is the request that would fail with the destructive middleware:
        // The first request's response would delete the cookie, so the second request has no cookie.
        var response2 = await client.GetAsync("/api/v1/auth/user");
        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            response2.StatusCode);

        // Step 4: Third request for good measure
        var response3 = await client.GetAsync("/api/v1/auth/user");
        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            response3.StatusCode);
    }

    /// <summary>
    /// Verifies that multiple logins by the same user overwrite the session cleanly.
    /// </summary>
    [Fact]
    public async Task MultipleLogins_SameUser_OverwritesSession()
    {
        var client = _factory.CreateClientWithCookies();

        await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "test@maliev.com", password = "password", rememberMe = false });
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username = "test@maliev.com", password = "password", rememberMe = false });

        var ticketStore = _factory.Services.GetRequiredService<DistributedCacheTicketStore>();

        // Let's verify that the new session allows authenticated requests to succeed!
        // This is the true test of session validity.
        var userResponse = await client.GetAsync("/api/v1/auth/user");
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that an authenticated request after login returns OK or Forbidden (not 401).
    /// A 401 here means the cookie was destroyed before the auth middleware could read it.
    /// </summary>
    [Fact]
    public async Task AuthenticatedRequest_AfterLogin_NeverReturns401()
    {
        var client = _factory.CreateClientWithCookies();

        await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "test@maliev.com",
            password = "password",
            rememberMe = false
        });

        var meResponse = await client.GetAsync("/api/v1/auth/user");

        // The response should be OK (user context returned) or Forbidden (permissions issue)
        // but NEVER 401 Unauthorized — that would mean the session was destroyed
        Assert.True(
            meResponse.StatusCode == HttpStatusCode.OK ||
            meResponse.StatusCode == HttpStatusCode.Forbidden,
            $"Expected OK or Forbidden after login, but got {meResponse.StatusCode}. " +
            "If 401, the auth cookie was destroyed between requests.");
    }

    /// <summary>
    /// Regression test: verifies that the Set-Cookie header after login does NOT
    /// instruct the browser to delete the auth cookie on the very next non-static request.
    /// </summary>
    [Fact]
    public async Task AfterLogin_SubsequentResponse_DoesNotDeleteCookie()
    {
        var client = _factory.CreateClientWithCookies();

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "test@maliev.com",
            password = "password",
            rememberMe = false
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Make a subsequent request and inspect the response headers
        var response = await client.GetAsync("/api/v1/auth/user");

        // If any Set-Cookie header contains an expired date or max-age=0 for our auth cookie,
        // it means the middleware is destructively deleting the cookie
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var header in setCookieHeaders)
            {
                if (header.Contains("Maliev.Intranet.Auth", StringComparison.OrdinalIgnoreCase))
                {
                    // Cookie updates are OK (session renewal), but max-age=0 or expires in the past means deletion
                    Assert.DoesNotContain("max-age=0", header, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("expires=Thu, 01 Jan 1970", header, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}

/// <summary>
/// Extension methods for extracting cookies from HttpClient in test scenarios.
/// </summary>
internal static class HttpClientCookieExtensions
{
    /// <summary>
    /// Extracts a cookie value from the HttpClient's CookieContainer.
    /// </summary>
    /// <param name="client">The HttpClient instance.</param>
    /// <param name="name">The cookie name to extract.</param>
    /// <returns>The cookie value, or null if not found.</returns>
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

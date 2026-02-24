using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Maliev.Intranet.Tests;

/// <summary>Web application factory for BFF integration tests.</summary>
public class BffTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly RSA _testRsa = RSA.Create(2048);

    /// <summary>Configures the web host for testing.</summary>
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
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Mock authentication to bypass Google SSO in integration tests
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(_testRsa)
                };
            });

            // Allow derived classes to further configure services
            ConfigureAdditionalServices(services);
        });
    }

    /// <summary>Allows derived classes to configure additional services for testing.</summary>
    protected virtual void ConfigureAdditionalServices(IServiceCollection services)
    {
    }

    /// <summary>Creates a signed JWT test token with the specified user ID and permissions.</summary>
    public string CreateTestToken(string userId = "test-user", params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, $"{userId}@maliev.com"),
        };

        foreach (var p in permissions)
        {
            claims.Add(new Claim("permissions", p));
        }

        var key = new RsaSecurityKey(_testRsa);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}

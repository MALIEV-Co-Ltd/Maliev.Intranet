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

public class BffTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly RSA _testRsa = RSA.Create(2048);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-google-id",
                ["Authentication:Google:ClientSecret"] = "test-google-secret",
                ["Services:AuthService:BaseUrl"] = "http://auth-service",
                ["Services:CustomerService:BaseUrl"] = "http://customer-service",
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

    protected virtual void ConfigureAdditionalServices(IServiceCollection services)
    {
    }

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

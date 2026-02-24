using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Maliev.Intranet.Bff.Handlers;

/// <summary>
/// HTTP message handler that authenticates seed requests as actor 'system' in downstream audit logs.
/// </summary>
public class SeedServiceAccountHandler(IConfiguration configuration) : DelegatingHandler
{
    /// <summary>
    /// Generates a 'system' service account token and attaches it to the outgoing request.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var securityKey = configuration["Jwt:SecurityKey"]
            ?? throw new InvalidOperationException("Jwt:SecurityKey not configured.");

        var issuer = configuration["Jwt:Issuer"] ?? "https://api.maliev.com";
        var audience = configuration["Jwt:Audience"] ?? "https://api.maliev.com";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: new[]
            {
                new Claim("sub", "system"),
                new Claim("user_type", "service"),
                new Claim("role", "service-account"),
                new Claim("permissions", "*")
            },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        ));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Maliev.Intranet.Bff.Middleware;

/// <summary>
/// Middleware that enriches the current user's claims with roles and permissions from the JWT access token.
/// This keeps the authentication cookie small while ensuring authorization checks have access to all claims.
/// </summary>
public class JwtClaimsEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtClaimsEnrichmentMiddleware> _logger;
    private const string JwtClaimsCacheKey = "MalievJwtClaimsParsed";
    private const int TokenRefreshBufferSeconds = 60;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtClaimsEnrichmentMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next request delegate in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public JwtClaimsEnrichmentMiddleware(RequestDelegate next, ILogger<JwtClaimsEnrichmentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes an HTTP request and enriches user claims from JWT if authenticated.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only process authenticated requests
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Check if we've already enriched claims for this request
            if (!context.Items.ContainsKey(JwtClaimsCacheKey))
            {
                try
                {
                    // Try to get access token from authentication properties first
                    var accessToken = await context.GetTokenAsync("access_token");

                    // If not in auth properties, try to get from claims
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        accessToken = context.User.FindFirst("access_token")?.Value;
                    }

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        // Parse JWT and extract claims
                        var handler = new JwtSecurityTokenHandler();

                        try
                        {
                            var jwtToken = handler.ReadJwtToken(accessToken);

                            if (jwtToken.ValidTo <= DateTime.UtcNow.AddSeconds(TokenRefreshBufferSeconds))
                            {
                                var refreshedToken = await TryReExchangePlatformJwtAsync(context);
                                if (!string.IsNullOrEmpty(refreshedToken))
                                {
                                    accessToken = refreshedToken;
                                    jwtToken = handler.ReadJwtToken(accessToken);
                                }
                            }

                            if (jwtToken.ValidTo <= DateTime.UtcNow)
                            {
                                _logger.LogWarning(
                                    "Access token expired for user {UserId}. Authorization claims cannot be enriched.",
                                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                            }
                            else
                            {
                                var identity = context.User.Identity as ClaimsIdentity;

                                if (identity != null)
                                {
                                    // Add roles and permissions from JWT to current identity
                                    foreach (var claim in jwtToken.Claims.Where(c =>
                                        c.Type is "roles" or "role" or "permissions" or "permission"))
                                    {
                                        // Only add if not already present
                                        if (!identity.HasClaim(claim.Type, claim.Value))
                                        {
                                            identity.AddClaim(new Claim(claim.Type, claim.Value));
                                        }

                                        // Also add as ClaimTypes.Role for ASP.NET Core authorization
                                        if (claim.Type is "roles" or "role" && !identity.HasClaim(ClaimTypes.Role, claim.Value))
                                        {
                                            identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Token parsing failed - log but do NOT force signout
                            // The user's session is valid, just skip claim enrichment
                            _logger.LogWarning(ex,
                                "Failed to parse access token for user {UserId}. Skipping claim enrichment.",
                                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                        }
                    }
                    else
                    {
                        // No access token found - this is a stale cookie scenario
                        // Log warning but do NOT force signout - the session cookie may still be valid
                        // Downstream services will receive requests without a token and handle accordingly
                        _logger.LogWarning(
                            "No access token found for authenticated user {UserId}. Downstream calls will use no token.",
                            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                    }
                }
                catch (Exception ex)
                {
                    // Unexpected error during token retrieval - log but do NOT force signout
                    // The user's session may still be valid
                    _logger.LogError(ex,
                        "Failed to retrieve access token for user {UserId}. Skipping claim enrichment.",
                        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                }

                // Mark as processed to avoid re-parsing
                context.Items[JwtClaimsCacheKey] = true;
            }
        }

        await _next(context);
    }

    private async Task<string?> TryReExchangePlatformJwtAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("user_id")?.Value
            ?? context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            var email = context.User.FindFirst("email")?.Value ?? context.User.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = context.User.FindFirst("name")?.Value ?? context.User.FindFirst(ClaimTypes.Name)?.Value;
            var googleUserId = context.User.GetGoogleUserId();
            var picture = context.User.GetProfileImageUrl();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(googleUserId))
            {
                _logger.LogWarning("Cannot refresh platform JWT for user {UserId}: email or google_user_id is missing.", userId);
                return null;
            }

            var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            using var authClient = factory.CreateClient("AuthService");
            var exchangeResponse = await authClient.PostAsJsonAsync(
                "/auth/v1/exchange/google",
                new { email, full_name = fullName, google_user_id = googleUserId, profile_image_url = picture },
                context.RequestAborted);

            if (!exchangeResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AuthService exchange returned {StatusCode} during platform JWT refresh for user {UserId}.",
                    exchangeResponse.StatusCode,
                    userId);
                return null;
            }

            var result = await exchangeResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: context.RequestAborted);
            var newToken = result.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(newToken))
            {
                return null;
            }

            var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (authResult?.Properties is not null)
            {
                authResult.Properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = newToken }]);
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User, authResult.Properties);
            }

            var identity = context.User.Identity as ClaimsIdentity;
            var oldTokenClaim = identity?.FindFirst("access_token");
            if (oldTokenClaim is not null)
            {
                identity?.RemoveClaim(oldTokenClaim);
            }

            identity?.AddClaim(new Claim("access_token", newToken));
            _logger.LogInformation("Platform JWT refreshed before authorization for user {UserId}.", userId);
            return newToken;
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Platform JWT refresh failed before authorization for user {UserId}.", userId);
            return null;
        }
    }
}

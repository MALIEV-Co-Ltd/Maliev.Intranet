using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Maliev.Intranet.Bff;

/// <summary>
/// HTTP message handler that propagates user identity context to downstream services.
/// Proactively refreshes the platform JWT when it is expired or close to expiry,
/// and retries GET requests once on 401 after a forced refresh.
/// </summary>
public class UserContextHandler(IHttpContextAccessor httpContextAccessor, ILogger<UserContextHandler> logger) : DelegatingHandler
{
    /// <summary>Refresh the token if it expires within this many seconds.</summary>
    private const int TokenRefreshBufferSeconds = 60;

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            logger.LogWarning("HttpContext is null in UserContextHandler for request {Url}", request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            logger.LogDebug("User is not authenticated in UserContextHandler for request {Url}", request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }

        var userId = user.FindFirst("user_id")?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Resolve token — AuthProperties (step 2) is where OnTicketReceived stores it
        var accessToken = user.FindFirst("access_token")?.Value;
        if (string.IsNullOrEmpty(accessToken))
            accessToken = await httpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                accessToken = authHeader[7..];
        }

        // ── Proactive refresh: re-exchange before the JWT is used if it has expired ──
        if (!string.IsNullOrEmpty(accessToken) && IsPlatformJwtExpiredOrExpiringSoon(accessToken))
        {
            logger.LogInformation("Platform JWT expired or expiring soon for user {UserId}; refreshing before request to {Url}", userId, request.RequestUri);
            var refreshed = await TryReExchangePlatformJwtAsync(httpContext, userId, cancellationToken);
            if (!string.IsNullOrEmpty(refreshed))
                accessToken = refreshed;
        }

        if (!string.IsNullOrEmpty(accessToken))
        {
            logger.LogDebug("Attaching platform JWT for user {UserId} to request {Url}", userId, request.RequestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        else
        {
            logger.LogError("CRITICAL: Access token missing for authenticated user {UserId}. Request {Url} will likely fail with 401.", userId, request.RequestUri);
            logger.LogDebug("Available claims for user {UserId}: {Claims}", userId, string.Join(", ", user.Claims.Select(c => c.Type)));
        }

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            // ── Reactive fallback: token may have been stale despite the proactive check ──
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogWarning("Downstream 401 for {Method} {Url} despite proactive check; attempting one-shot re-exchange for user {UserId}", request.Method, request.RequestUri, userId);
                var retryToken = await TryReExchangePlatformJwtAsync(httpContext, userId, cancellationToken);
                if (!string.IsNullOrEmpty(retryToken))
                {
                    var retry = new HttpRequestMessage(request.Method, request.RequestUri);
                    foreach (var (k, v) in request.Headers)
                    {
                        if (!string.Equals(k, "Authorization", StringComparison.OrdinalIgnoreCase))
                            retry.Headers.TryAddWithoutValidation(k, v);
                    }
                    retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", retryToken);

                    var retryResponse = await base.SendAsync(retry, cancellationToken);
                    if (retryResponse.StatusCode != HttpStatusCode.Unauthorized)
                        return retryResponse;

                    logger.LogError("Retry after re-exchange still returned 401 for {Url}. User: {UserId}", request.RequestUri, userId);
                    return retryResponse;
                }
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                logger.LogError("Downstream 401 for {Url}. User: {UserId}. Token refresh unavailable or failed.", request.RequestUri, userId);
            else if (response.StatusCode == HttpStatusCode.Forbidden)
                logger.LogError("Downstream 403 for {Url}. User: {UserId} lacks the required IAM permission.", request.RequestUri, userId);

            return response;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Downstream request timed out: {Url}. User: {UserId}", request.RequestUri, userId);
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
            {
                RequestMessage = request,
                Content = new StringContent("The request to the downstream service timed out.")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error calling downstream service: {Url}. User: {UserId}", request.RequestUri, userId);
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = request,
                Content = new StringContent($"Error calling downstream service: {ex.Message}")
            };
        }
    }

    /// <summary>
    /// Re-exchanges the user's Google identity (claims already in cookie) with AuthService
    /// to obtain a fresh platform JWT, then updates the auth cookie in place.
    /// </summary>
    private async Task<string?> TryReExchangePlatformJwtAsync(HttpContext context, string? userId, CancellationToken ct)
    {
        try
        {
            var user = context.User;
            var email = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;
            var googleUserId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var fullName = user.FindFirst("name")?.Value ?? user.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleUserId))
            {
                logger.LogWarning("Cannot re-exchange: email or google_user_id missing for user {UserId}", userId);
                return null;
            }

            var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            using var authClient = factory.CreateClient("AuthService");

            var exchangeResponse = await authClient.PostAsJsonAsync(
                "/auth/v1/exchange/google",
                new { email, full_name = fullName, google_user_id = googleUserId },
                ct);

            if (!exchangeResponse.IsSuccessStatusCode)
            {
                logger.LogError("AuthService exchange returned {Status} during token refresh for user {UserId}", exchangeResponse.StatusCode, userId);
                return null;
            }

            var result = await exchangeResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            var newToken = result.GetProperty("access_token").GetString();
            if (string.IsNullOrEmpty(newToken)) return null;

            // Persist the new token back into the auth cookie so subsequent requests use it
            var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (authResult?.Properties != null)
            {
                authResult.Properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = newToken }]);
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User, authResult.Properties);
            }

            logger.LogInformation("Platform JWT refreshed via re-exchange for user {UserId}", userId);
            return newToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Platform JWT re-exchange failed for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Returns true when the JWT's <c>exp</c> claim is at or before now + <see cref="TokenRefreshBufferSeconds"/>.
    /// Non-parseable tokens are treated as expired.
    /// </summary>
    private static bool IsPlatformJwtExpiredOrExpiringSoon(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return true;
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo <= DateTime.UtcNow.AddSeconds(TokenRefreshBufferSeconds);
        }
        catch
        {
            return true;
        }
    }
}

using System.Security.Claims;

namespace Maliev.Intranet.Bff;

/// <summary>
/// HTTP message handler that propagates user identity context to downstream services.
/// </summary>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
/// <param name="logger">The logger instance.</param>
public class UserContextHandler(IHttpContextAccessor httpContextAccessor, ILogger<UserContextHandler> logger) : DelegatingHandler
{
    /// <summary>
    /// intercepts outgoing requests to add identity headers.
    /// </summary>
    /// <param name="request">The outgoing request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response.</returns>
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
            logger.LogWarning("User is not authenticated in UserContextHandler for request {Url}", request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId) && !request.Headers.Contains("X-User-Id"))
        {
            request.Headers.Add("X-User-Id", userId);
        }

        // Retrieve the JWT from the user's claims and attach as Bearer token
        var accessToken = user.FindFirst("access_token")?.Value;
        if (!string.IsNullOrEmpty(accessToken))
        {
            if (request.Headers.Authorization == null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
        }
        else
        {
            logger.LogWarning("Access token is missing in user claims for user {UserId}", userId);

            // Log available claims for debugging
            var claimTypes = user.Claims.Select(c => c.Type).ToList();
            logger.LogDebug("Available claims for user {UserId}: {Claims}", userId, string.Join(", ", claimTypes));
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            logger.LogError("Downstream service returned 401 Unauthorized for {Url}. User: {UserId}", request.RequestUri, userId);
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            logger.LogError("Downstream service returned 403 Forbidden for {Url}. User: {UserId}", request.RequestUri, userId);
        }

        return response;
    }
}
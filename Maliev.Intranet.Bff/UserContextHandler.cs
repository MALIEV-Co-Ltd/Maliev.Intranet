using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

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

        var userId = user.FindFirst("user_id")?.Value ?? user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId) && !request.Headers.Contains("X-User-Id"))
        {
            request.Headers.Add("X-User-Id", userId);
        }

        // 1. Try to get token from claims (access_token claim)
        var accessToken = user.FindFirst("access_token")?.Value;

        // 2. Try to get token from AuthenticationProperties (requires SaveTokens = true)
        if (string.IsNullOrEmpty(accessToken))
        {
            accessToken = await httpContext.GetTokenAsync("access_token");
        }

        // 3. Fallback: Check if the incoming request already has a Bearer token
        if (string.IsNullOrEmpty(accessToken))
        {
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                accessToken = authHeader[7..];
            }
        }


        if (!string.IsNullOrEmpty(accessToken))
        {
            if (request.Headers.Authorization == null)
            {
                logger.LogDebug("Attaching platform JWT for user {UserId} to request {Url}", userId, request.RequestUri);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }
        }
        else
        {
            logger.LogError("CRITICAL: Access token is missing in user claims for authenticated user {UserId}. Downstream calls will likely fail with 401.", userId);

            // Log available claims for debugging
            var claimTypes = user.Claims.Select(c => c.Type).ToList();
            logger.LogDebug("Available claims for user {UserId}: {Claims}", userId, string.Join(", ", claimTypes));
        }


        try
        {
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
        catch (OperationCanceledException ex)
        {
            var isTimeout = !cancellationToken.IsCancellationRequested;
            var cancellationSource = isTimeout ? "HttpClient timeout" : "External cancellation (HttpContext or Client)";
            
            logger.LogWarning(ex, "Request to downstream service was CANCELED [{Source}]: {Url}. User: {UserId}. IsTimeout: {IsTimeout}", 
                cancellationSource, request.RequestUri, userId, isTimeout);

            // Return a 504 Gateway Timeout instead of letting the exception bubble up
            return new HttpResponseMessage(System.Net.HttpStatusCode.GatewayTimeout)
            {
                RequestMessage = request,
                Content = new StringContent($"The request to the downstream service was canceled ({cancellationSource}).")
            };
        }
        catch (Exception ex)
        {
            // Return a 503 Service Unavailable for other network errors
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = request,
                Content = new StringContent($"Error calling downstream service: {ex.Message}")
            };
        }
    }
}
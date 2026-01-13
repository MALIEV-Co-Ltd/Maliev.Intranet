using System.Security.Claims;

namespace Maliev.Intranet.Bff;

/// <summary>
/// HTTP message handler that propagates user identity context to downstream services.
/// </summary>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
public class UserContextHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    /// <summary>
    /// intercepts outgoing requests to add identity headers.
    /// </summary>
    /// <param name="request">The outgoing request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Propagate user identity to downstream services for auditability
            request.Headers.Add("X-User-Id", userId);
        }

        // Also propagate authorization header if present
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
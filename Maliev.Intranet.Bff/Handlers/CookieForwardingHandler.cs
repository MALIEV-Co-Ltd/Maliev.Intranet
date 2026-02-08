using Microsoft.AspNetCore.Http;

namespace Maliev.Intranet.Bff.Handlers;

/// <summary>
/// HTTP message handler that forwards authentication cookies and headers from the current HTTP request
/// to outgoing requests. This is essential for Blazor Server components calling the BFF's own API.
/// </summary>
public class CookieForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    /// <summary>
    /// intercepts outgoing requests to add identity headers from the current context.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context != null)
        {
            // Forward all cookies
            var cookieHeader = context.Request.Headers.Cookie.ToString();
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }

            // Forward Authorization header (for Bearer tokens)
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) && !request.Headers.Contains("Authorization"))
            {
                request.Headers.Add("Authorization", authHeader);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

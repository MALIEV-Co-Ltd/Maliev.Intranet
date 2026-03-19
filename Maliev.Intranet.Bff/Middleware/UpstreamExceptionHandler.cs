using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Middleware;

/// <summary>Handles <see cref="HttpRequestException"/> from upstream services and returns a 503 problem details response.</summary>
/// <param name="logger">Logger for recording upstream failures.</param>
public sealed class UpstreamExceptionHandler(ILogger<UpstreamExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is not HttpRequestException httpEx) return false;

        logger.LogWarning(httpEx, "Upstream service unavailable: {Message}", httpEx.Message);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Upstream service unavailable",
            Detail = "A downstream service did not respond in time. Please retry later."
        };

        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}

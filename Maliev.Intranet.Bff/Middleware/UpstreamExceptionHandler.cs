using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.Intranet.Bff.Middleware;

/// <summary>
/// Handles exceptions from upstream service calls:
/// <list type="bullet">
/// <item><see cref="HttpRequestException"/> — upstream unreachable or returned an error; mapped to 503.</item>
/// <item><see cref="OperationCanceledException"/> / <see cref="TaskCanceledException"/> — client cancelled
/// the request (e.g. Blazor component disposed mid-fetch); swallowed silently with 499.</item>
/// </list>
/// </summary>
/// <param name="logger">Logger for recording upstream failures.</param>
public sealed class UpstreamExceptionHandler(ILogger<UpstreamExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        // Client closed the connection (Blazor component disposed, navigation, etc.).
        // The request cancellation token fires and propagates through downstream HttpClient calls.
        // Nothing to write — the client is gone. Suppress the exception so it is not logged as a 500.
        if (ex is OperationCanceledException && ctx.RequestAborted.IsCancellationRequested)
        {
            ctx.Response.StatusCode = 499; // Client Closed Request (nginx convention)
            return true;
        }

        // HttpClient/Resilience pipeline timeout — client is still waiting but downstream did not respond in time.
        // TaskCanceledException is a subclass of OperationCanceledException; both fire on timeout.
        if (ex is OperationCanceledException && !ctx.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Upstream request timed out: {Message}", ex.Message);

            var timeoutProblem = new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "Upstream request timed out",
                Detail = "A downstream service did not respond within the allowed time. Please retry later."
            };

            ctx.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await ctx.Response.WriteAsJsonAsync(timeoutProblem, ct);
            return true;
        }

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

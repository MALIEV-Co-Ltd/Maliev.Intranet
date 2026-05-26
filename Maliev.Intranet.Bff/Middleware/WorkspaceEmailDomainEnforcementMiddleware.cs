using Maliev.Intranet.Bff.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Maliev.Intranet.Bff.Middleware;

internal sealed class WorkspaceEmailDomainEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WorkspaceEmailDomainEnforcementMiddleware> _logger;

    public WorkspaceEmailDomainEnforcementMiddleware(RequestDelegate next, ILogger<WorkspaceEmailDomainEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true ||
            IsAuthenticationPath(context.Request.Path) ||
            WorkspaceEmailDomainPolicy.IsAllowedEmployee(context.User))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst("user_id")?.Value ??
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogWarning(
            "Rejected Intranet access for authenticated principal {UserId} with non-workspace email. Path: {Path}",
            userId,
            context.Request.Path);

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (ShouldRedirectToLogin(context.Request.Path))
        {
            context.Response.Redirect($"/login?error={Uri.EscapeDataString(WorkspaceEmailDomainPolicy.UnauthorizedDomainMessage)}");
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }

    private static bool IsAuthenticationPath(PathString path)
    {
        return path.StartsWithSegments("/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/signin-google", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/v1/auth/logout", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldRedirectToLogin(PathString path)
    {
        return !path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
    }
}

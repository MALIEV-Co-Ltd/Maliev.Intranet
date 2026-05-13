using Microsoft.AspNetCore.Authorization;

namespace Maliev.Intranet.Client.Authorization;

/// <summary>
/// Evaluates MALIEV permission requirements against browser authentication claims.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var permissions = context.User.Claims
            .Where(c => c.Type is
                "permissions" or
                "permission" or
                "role" or
                "roles" or
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(c => c.Value);

        if (PermissionMatcher.Match(requirement.Permission, permissions))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

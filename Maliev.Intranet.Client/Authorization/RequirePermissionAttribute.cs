using Microsoft.AspNetCore.Authorization;

namespace Maliev.Intranet.Client.Authorization;

/// <summary>
/// Declarative permission-based authorization for Blazor components.
/// This matches the server-side RequirePermissionAttribute for consistency.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"Permission:{permission}";
    }
}

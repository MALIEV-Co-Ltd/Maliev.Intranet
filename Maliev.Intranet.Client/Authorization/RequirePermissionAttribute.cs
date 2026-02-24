using Microsoft.AspNetCore.Authorization;

namespace Maliev.Intranet.Client.Authorization;

/// <summary>
/// Declarative permission-based authorization for Blazor components.
/// This matches the server-side RequirePermissionAttribute for consistency.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="permission">The required permission identifier.</param>
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"Permission:{permission}";
    }
}

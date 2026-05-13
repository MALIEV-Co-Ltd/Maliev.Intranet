using Microsoft.AspNetCore.Authorization;

namespace Maliev.Intranet.Client.Authorization;

/// <summary>
/// Authorization requirement for a MALIEV permission policy.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRequirement"/> class.
    /// </summary>
    /// <param name="permission">The required permission.</param>
    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }

    /// <summary>
    /// Gets the permission required by the policy.
    /// </summary>
    public string Permission { get; }
}

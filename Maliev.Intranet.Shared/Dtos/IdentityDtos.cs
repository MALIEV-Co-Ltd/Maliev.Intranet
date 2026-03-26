namespace Maliev.Intranet.Shared;

/// <summary>
/// Data transfer object for basic user context information, used for session and identity tracking.
/// </summary>
public class UserContextDto
{
    /// <summary>The unique identifier of the user in the identity system.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>The display name of the user (e.g., "John Doe").</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>The login username of the user.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>The primary email address associated with the user account.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>List of security roles assigned to the user.</summary>
    public List<string> Roles { get; set; } = new();
    /// <summary>List of granular permissions granted to the user, either directly or via roles.</summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// DTO representing a granular permission within the system's access control model.
/// </summary>
public class PermissionDto
{
    /// <summary>The unique identifier for the permission (e.g., "users.create").</summary>
    public string PermissionId { get; set; } = string.Empty;
    /// <summary>The human-readable name of the permission.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>A detailed description of what this permission allows.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>The functional category this permission belongs to (e.g., "User Management").</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing a security role, which groups multiple permissions together.
/// </summary>
public class RoleDto
{
    /// <summary>The unique identifier for the role (e.g., "Admin").</summary>
    public string RoleId { get; set; } = string.Empty;
    /// <summary>The display name of the role.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>A detailed description of the role's purpose and scope.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>List of permission names associated with this role.</summary>
    public List<string> Permissions { get; set; } = new();
    /// <summary>List of unique identifiers for the permissions associated with this role.</summary>
    public List<string> PermissionIds { get; set; } = new();
}

/// <summary>
/// Request model for assigning roles and permissions to a specific user.
/// </summary>
public class UserAssignmentRequest
{
    /// <summary>The identifier of the user to be updated.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>List of role identifiers to assign to the user.</summary>
    public List<string> Roles { get; set; } = new();
    /// <summary>List of specific permission identifiers to grant to the user.</summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Summary information for an Identity and Access Management (IAM) principal.
/// </summary>
public class PrincipalSummaryDto
{
    /// <summary>The unique identifier for the internal principal record.</summary>
    public Guid Id { get; set; }
    /// <summary>The unique identifier for the principal in the external identity provider.</summary>
    public Guid PrincipalId { get; set; }
    /// <summary>The type of principal (e.g., User, ServiceAccount).</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>The unique login identifier or email for the principal.</summary>
    public string Identifier { get; set; } = string.Empty;
    /// <summary>The display name associated with the principal.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>The primary contact email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Indicates if the principal account is globally enabled.</summary>
    public bool IsEnabled { get; set; }
    /// <summary>Indicates if the principal is currently active and within its valid date range.</summary>
    public bool IsActive { get; set; }
    /// <summary>The date and time when the principal record was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request model for granting a specific role to an IAM principal.
/// </summary>
public class GrantRoleRequestDto
{
    /// <summary>The unique identifier of the role to be granted.</summary>
    public string RoleId { get; set; } = string.Empty;
    /// <summary>The name of the role for display and validation purposes.</summary>
    public string RoleName { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing a role binding, which connects a principal to a role for a specific resource.
/// </summary>
public class RoleBindingDto
{
    /// <summary>The unique identifier for the role binding record.</summary>
    public string BindingId { get; set; } = string.Empty;
    /// <summary>The identifier of the role being bound.</summary>
    public string RoleId { get; set; } = string.Empty;
    /// <summary>The resource path or scope the role applies to (e.g., "customers/123").</summary>
    public string ResourcePath { get; set; } = string.Empty;
    /// <summary>The identifier of the principal receiving the role.</summary>
    public Guid PrincipalId { get; set; }
    /// <summary>The name of the role being bound.</summary>
    public string RoleName { get; set; } = string.Empty;
    /// <summary>The date and time when the role was granted.</summary>
    public DateTime GrantedAt { get; set; }
}

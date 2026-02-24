namespace Maliev.Intranet.Shared;

/// <summary>
/// DTO for basic user context information.
/// </summary>
public class UserContextDto
{
    /// <summary>Gets or sets the unique user identifier.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the username.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>Gets or sets the email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets the collection of assigned roles.</summary>
    public List<string> Roles { get; set; } = new();
    /// <summary>Gets or sets the collection of granted permissions.</summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// DTO representing a permission in the system.
/// </summary>
public class PermissionDto
{
    /// <summary>Gets or sets the unique permission identifier (e.g., order.read).</summary>
    public string PermissionId { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the functional category.</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing a security role.
/// </summary>
public class RoleDto
{
    /// <summary>Gets or sets the unique role identifier.</summary>
    public string RoleId { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the detailed description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the collection of granted permission names.</summary>
    public List<string> Permissions { get; set; } = new();
    /// <summary>Gets or sets the collection of granted permission identifiers.</summary>
    public List<string> PermissionIds { get; set; } = new();
}

/// <summary>
/// Request for assigning roles/permissions to a user.
/// </summary>
public class UserAssignmentRequest
{
    /// <summary>Gets or sets the target user identifier.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>Gets or sets the collection of roles to assign.</summary>
    public List<string> Roles { get; set; } = new();
    /// <summary>Gets or sets the collection of direct permissions to grant.</summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Summary information for an IAM principal.
/// </summary>
public class PrincipalSummaryDto
{
    /// <summary>Gets or sets the IAM principal identifier.</summary>
    public Guid PrincipalId { get; set; }
    /// <summary>Gets or sets the type of principal (e.g., User, ServiceAccount).</summary>
    public string PrincipalType { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the email address.</summary>
    public string? Email { get; set; }
    /// <summary>Gets or sets a value indicating whether the principal is currently active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for granting a role to a principal.
/// </summary>
public class GrantRoleRequestDto
{
    /// <summary>Gets or sets the role identifier.</summary>
    public string RoleId { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing a binding between a principal and a role.
/// </summary>
public class RoleBindingDto
{
    /// <summary>Gets or sets the unique binding identifier.</summary>
    public Guid BindingId { get; set; }
    /// <summary>Gets or sets the role identifier.</summary>
    public string RoleId { get; set; } = string.Empty;
    /// <summary>Gets or sets the resource path where this binding applies.</summary>
    public string ResourcePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the target principal ID.</summary>
    public Guid PrincipalId { get; set; }
    /// <summary>Gets or sets when the role was granted.</summary>
    public DateTime GrantedAt { get; set; }
}

namespace Maliev.Intranet.Shared;

/// <summary>
/// DTO for basic user context information.
/// </summary>
public class UserContextDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// DTO representing a permission in the system.
/// </summary>
public class PermissionDto
{
    public string PermissionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing a security role.
/// </summary>
public class RoleDto
{
    public string RoleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public List<string> PermissionIds { get; set; } = new();
}

/// <summary>
/// Request for assigning roles/permissions to a user.
/// </summary>
public class UserAssignmentRequest
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Summary information for an IAM principal.
/// </summary>
public class PrincipalSummaryDto
{
    public Guid Id { get; set; }
    public Guid PrincipalId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for granting a role to a principal.
/// </summary>
public class GrantRoleRequestDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing a binding between a principal and a role.
/// </summary>
public class RoleBindingDto
{
    public string BindingId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string ResourcePath { get; set; } = string.Empty;
    public Guid PrincipalId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
}

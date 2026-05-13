using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Maliev.Intranet.Client.Authorization;

/// <summary>
/// Creates authorization policies for MALIEV permission policy names.
/// </summary>
public sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "Permission:";

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionAuthorizationPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return base.GetPolicyAsync(policyName);
        }

        var policyContent = policyName[PolicyPrefix.Length..];
        if (string.IsNullOrWhiteSpace(policyContent))
        {
            throw new InvalidOperationException("Permission policy names must include a permission.");
        }

        var permission = policyContent.Split(':', StringSplitOptions.RemoveEmptyEntries)[0];
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new InvalidOperationException("Permission policy names must include a permission.");
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

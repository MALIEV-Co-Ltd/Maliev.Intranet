namespace Maliev.Intranet.Client.Authorization;

/// <summary>
/// Matches required permissions against permission claims available in the browser authentication state.
/// </summary>
public static class PermissionMatcher
{
    /// <summary>
    /// Determines whether any granted permission satisfies the required permission.
    /// </summary>
    /// <param name="requiredPermission">The permission required by the route or component.</param>
    /// <param name="grantedPermissions">The user's granted permissions and role-like claims.</param>
    /// <returns><see langword="true"/> when access should be granted.</returns>
    public static bool Match(string requiredPermission, IEnumerable<string> grantedPermissions)
    {
        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            return false;
        }

        var permissions = grantedPermissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (permissions.Count == 0)
        {
            return false;
        }

        if (permissions.Any(p =>
                string.Equals(p, "platform.owner", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p, "roles.platform.owner", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return permissions.Any(permission => IsMatch(requiredPermission, permission));
    }

    /// <summary>
    /// Determines whether a single granted permission satisfies the required permission.
    /// </summary>
    /// <param name="required">The required permission.</param>
    /// <param name="granted">The granted permission claim.</param>
    /// <returns><see langword="true"/> when the granted permission matches.</returns>
    public static bool IsMatch(string required, string granted)
    {
        if (string.IsNullOrWhiteSpace(required) || string.IsNullOrWhiteSpace(granted))
        {
            return false;
        }

        var normalizedRequired = NormalizePolicyPrefix(required);
        var normalizedGranted = NormalizePolicyPrefix(granted);

        if (string.Equals(normalizedRequired, normalizedGranted, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var requiredParts = normalizedRequired.Split('.');
        var grantedParts = normalizedGranted.Split('.');

        if (grantedParts.Length == 1 && grantedParts[0] == "*")
        {
            return true;
        }

        for (var index = 0; index < grantedParts.Length; index++)
        {
            if (grantedParts[index] == "*")
            {
                return index == grantedParts.Length - 1;
            }

            if (index >= requiredParts.Length)
            {
                return false;
            }

            if (!string.Equals(requiredParts[index], grantedParts[index], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return grantedParts.Length == requiredParts.Length;
    }

    private static string NormalizePolicyPrefix(string permission)
    {
        const string prefix = "Permission:";

        return permission.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? permission[prefix.Length..]
            : permission;
    }
}

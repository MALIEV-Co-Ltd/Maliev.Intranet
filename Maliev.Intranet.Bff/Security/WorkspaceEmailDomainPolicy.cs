using System.Security.Claims;

namespace Maliev.Intranet.Bff.Security;

internal static class WorkspaceEmailDomainPolicy
{
    internal const string UnauthorizedDomainMessage = "Use your @maliev.com workspace email to sign in.";

    private const string AllowedDomain = "maliev.com";

    internal static bool IsAllowedEmployee(ClaimsPrincipal? principal)
    {
        return IsAllowedEmployeeEmail(FindEmail(principal));
    }

    internal static bool IsAllowedEmployeeEmail(string? email)
    {
        var normalizedEmail = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            normalizedEmail.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var atIndex = normalizedEmail.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 0 ||
            atIndex != normalizedEmail.LastIndexOf('@') ||
            atIndex == normalizedEmail.Length - 1)
        {
            return false;
        }

        var domain = normalizedEmail[(atIndex + 1)..];
        return string.Equals(domain, AllowedDomain, StringComparison.OrdinalIgnoreCase);
    }

    internal static string? FindEmail(ClaimsPrincipal? principal)
    {
        return principal?.FindFirst("email")?.Value ??
            principal?.FindFirst(ClaimTypes.Email)?.Value;
    }
}

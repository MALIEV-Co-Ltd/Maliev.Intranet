using System;
using System.Linq;
using System.Security.Claims;

namespace Maliev.Intranet.Bff;

/// <summary>
/// Shared helpers for resolving identity claims that may appear under provider-specific
/// claim types across cookie and JWT payload flows.
/// </summary>
public static class IdentityClaimsExtensions
{
    private static readonly string[] PictureClaimTypes =
    [
        "picture",
        "urn:google:picture",
        "urn:google:avatar",
        "http://schemas.openid.net/claims/picture"
    ];

    /// <summary>Gets the best-available profile image URL from the principal.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The profile image URL, if present.</returns>
    public static string? GetProfileImageUrl(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        foreach (var claimType in PictureClaimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return principal.Claims
            .Where(c => !string.IsNullOrWhiteSpace(c.Value))
            .FirstOrDefault(c =>
                c.Type.EndsWith("/picture", StringComparison.OrdinalIgnoreCase) ||
                c.Type.EndsWith(":picture", StringComparison.OrdinalIgnoreCase) ||
                c.Type.Equals("profile_image_url", StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    /// <summary>Gets the Google principal identifier used by AuthService exchange requests.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The Google user identifier when available.</returns>
    public static string? GetGoogleUserId(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        return principal.FindFirst("google_user_id")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

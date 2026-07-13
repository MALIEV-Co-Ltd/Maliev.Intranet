namespace Maliev.Intranet.Bff.Security;

/// <summary>Validates authorization feature flags before the BFF starts accepting requests.</summary>
public static class AuthorizationSafetyConfiguration
{
    /// <summary>
    /// Rejects configurations that could turn an authoritative resource check into fail-open access.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when resource-scoped authorization and IAM fail-open behavior are both enabled.
    /// </exception>
    public static void EnsureSafe(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Features:ResourceScopedAuthEnabled"))
        {
            throw new InvalidOperationException(
                "Features:ResourceScopedAuthEnabled must be true because the BFF exposes resource-scoped endpoints.");
        }

        if (configuration.GetValue<bool>("Features:FailOpenOnIAMError"))
        {
            throw new InvalidOperationException(
                "Features:FailOpenOnIAMError must remain false when resource-scoped authorization is enabled.");
        }
    }
}

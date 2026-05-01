using Asp.Versioning;

namespace Maliev.Intranet.Bff.Extensions;

/// <summary>
/// Extension methods for configuring BFF API versioning.
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Adds the MALIEV default API versioning configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddDefaultApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        return services;
    }
}

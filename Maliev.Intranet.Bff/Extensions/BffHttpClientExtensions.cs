using Microsoft.Extensions.Http.Resilience;

namespace Maliev.Intranet.Bff.Extensions;

/// <summary>
/// Extension methods for registering BFF-specific HTTP clients with user context forwarding.
/// </summary>
public static class BffHttpClientExtensions
{
    /// <summary>
    /// Registers a typed HTTP client for a BFF service with user context forwarding,
    /// service discovery, and standard resilience.
    /// </summary>
    /// <typeparam name="TClient">The typed HTTP client class.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="serviceName">The logical service name used for config lookup and service discovery.</param>
    /// <param name="configureClient">Optional additional client configuration.</param>
    public static void AddBffServiceClient<TClient>(
        this IHostApplicationBuilder builder,
        string serviceName,
        Action<HttpClient>? configureClient = null)
        where TClient : class
    {
        builder.Services.AddHttpClient<TClient>(serviceName, (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var explicitUrl = config[$"Services:{serviceName}:BaseUrl"];
            if (!string.IsNullOrEmpty(explicitUrl))
                client.BaseAddress = new Uri(explicitUrl);
            else
                client.BaseAddress = new Uri($"http://{serviceName}");

            client.Timeout = TimeSpan.FromSeconds(90);
            configureClient?.Invoke(client);
        })
        .AddHttpMessageHandler<UserContextHandler>()
        .AddServiceDiscovery()
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(70);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(150);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
        });
    }
}

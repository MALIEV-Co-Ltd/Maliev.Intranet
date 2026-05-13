using Microsoft.Extensions.Http.Resilience;

namespace Maliev.Intranet.Bff.Extensions;

/// <summary>
/// Extension methods for registering BFF-specific HTTP clients with user context forwarding.
/// </summary>
public static class BffHttpClientExtensions
{
    // Standard fast-service resilience: 3 retries, 30 s per attempt, 180 s total.
    // AttemptTimeout (30 s) × (1 + MaxRetryAttempts 3) = 120 s < TotalRequestTimeout 180 s ✓
    private static void ConfigureStandardResilience(HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(180);
        options.Retry.MaxRetryAttempts = 3;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(300);
    }

    /// <summary>
    /// Registers a typed HTTP client for a BFF service with an interface, user context forwarding,
    /// service discovery, and standard resilience.
    /// </summary>
    /// <typeparam name="TInterface">The service interface.</typeparam>
    /// <typeparam name="TClient">The implementation class.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="serviceName">The logical service name used for config lookup and service discovery.</param>
    /// <param name="configureClient">Optional additional client configuration.</param>
    public static void AddBffServiceClient<TInterface, TClient>(
        this IHostApplicationBuilder builder,
        string serviceName,
        Action<HttpClient>? configureClient = null)
        where TInterface : class
        where TClient : class, TInterface
    {
        builder.Services.AddHttpClient<TInterface, TClient>(serviceName, (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var explicitUrl = config[$"Services:{serviceName}:BaseUrl"];
            client.BaseAddress = !string.IsNullOrEmpty(explicitUrl)
                ? new Uri(explicitUrl)
                : new Uri($"https+http://{serviceName}");

            // HttpClient.Timeout must not be set when using resilience handlers —
            // it fires as TaskCanceledException that bypasses the resilience pipeline.
            // TotalRequestTimeout in the resilience options acts as the outer bound instead.
            client.Timeout = Timeout.InfiniteTimeSpan;
            configureClient?.Invoke(client);
        })
        .AddHttpMessageHandler<UserContextHandler>()
        .AddServiceDiscovery()
        .AddStandardResilienceHandler(ConfigureStandardResilience);
    }

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
            client.BaseAddress = !string.IsNullOrEmpty(explicitUrl)
                ? new Uri(explicitUrl)
                : new Uri($"https+http://{serviceName}");

            client.Timeout = Timeout.InfiniteTimeSpan;
            configureClient?.Invoke(client);
        })
        .AddHttpMessageHandler<UserContextHandler>()
        .AddServiceDiscovery()
        .AddStandardResilienceHandler(ConfigureStandardResilience);
    }

    /// <summary>
    /// Registers a typed HTTP client for a long-running BFF service (no retries, high timeouts).
    /// Use this for services that run expensive, non-idempotent operations like DFM analysis.
    /// </summary>
    public static void AddBffLongRunningServiceClient<TClient>(
        this IHostApplicationBuilder builder,
        string serviceName,
        TimeSpan? attemptTimeout = null,
        Action<HttpClient>? configureClient = null)
        where TClient : class
    {
        var perAttempt = attemptTimeout ?? TimeSpan.FromSeconds(300);

        // SamplingDuration must be ≥ 2 × AttemptTimeout (framework validation constraint).
        var samplingDuration = perAttempt + perAttempt;
        // TotalRequestTimeout must comfortably cover 1 retry (minimum MaxRetryAttempts = 1).
        var totalTimeout = perAttempt + perAttempt + TimeSpan.FromSeconds(30);

        builder.Services.AddHttpClient<TClient>(serviceName, (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var explicitUrl = config[$"Services:{serviceName}:BaseUrl"];
            client.BaseAddress = !string.IsNullOrEmpty(explicitUrl)
                ? new Uri(explicitUrl)
                : new Uri($"https+http://{serviceName}");

            client.Timeout = Timeout.InfiniteTimeSpan;
            configureClient?.Invoke(client);
        })
        .AddHttpMessageHandler<UserContextHandler>()
        .AddServiceDiscovery()
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = perAttempt;
            options.TotalRequestTimeout.Timeout = totalTimeout;
            options.Retry.MaxRetryAttempts = 1;  // framework min is 1; expensive ops rarely benefit from retrying
            options.CircuitBreaker.SamplingDuration = samplingDuration;
        });
    }
}

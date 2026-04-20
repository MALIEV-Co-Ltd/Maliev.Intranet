using Maliev.Intranet.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Maliev.Intranet.Tests.Bff.Hubs;

/// <summary>
/// Derived factory that strips service discovery DelegatingHandlers
/// and removes MassTransit bus to prevent RabbitMQ connection attempts
/// during WebApplicationFactory startup.
/// </summary>
public class SignalRTestFactory : BffTestWebApplicationFactory
{
    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        // Remove MassTransit bus/IBus registration to prevent RabbitMQ connection attempts
        // The SignalR negotiate endpoint tests don't need the message bus.
        // We remove both IBus and IBusControl implementations.
        var busDescriptors = services.Where(d =>
            d.ServiceType == typeof(MassTransit.IBus) ||
            d.ServiceType == typeof(MassTransit.IBusControl) ||
            (d.ServiceType.Namespace?.StartsWith("MassTransit") == true && d.ServiceType.Name.Contains("Bus"))).ToList();

        foreach (var descriptor in busDescriptors)
        {
            services.Remove(descriptor);
        }

        // Also remove MassTransit hosted services that start the bus
        var hostedServiceDescriptors = services
            .Where(d => d.ImplementationType?.Namespace?.StartsWith("MassTransit") == true ||
                       d.ServiceType?.Namespace?.StartsWith("MassTransit") == true)
            .ToList();

        foreach (var descriptor in hostedServiceDescriptors)
        {
            services.Remove(descriptor);
        }

        // Strip all DelegatingHandlers (service discovery, resilience, auth)
        // from every named HTTP client, then inject mock handler.
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Clear();
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = new MockHttpMessageHandler();
            });
        });
    }
}

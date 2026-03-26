using Maliev.Intranet.Bff.Hubs;
using Maliev.MessagingContracts.Contracts.Pricing;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="PriceCalculatedEvent"/> messages published by PricingService.
/// Broadcasts pricing results to connected Blazor clients via SignalR when a StoragePath is present.
/// </summary>
public class PriceCalculatedConsumer : IConsumer<PriceCalculatedEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<PriceCalculatedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PriceCalculatedConsumer"/>.
    /// </summary>
    public PriceCalculatedConsumer(
        IHubContext<NotificationHub> hub,
        ILogger<PriceCalculatedConsumer> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming <see cref="PriceCalculatedEvent"/> and pushes results via SignalR.
    /// Events without a StoragePath (non-file flows) are ignored.
    /// </summary>
    /// <param name="context">The MassTransit consume context.</param>
    public async Task Consume(ConsumeContext<PriceCalculatedEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("PriceCalculatedConsumer: received null message or payload");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogInformation(
            "PriceCalculatedConsumer: received event for storagePath={StoragePath}, unitPrice={UnitPrice}",
            payload.StoragePath, payload.TotalUnitPrice);

        if (string.IsNullOrEmpty(payload.StoragePath))
        {
            _logger.LogInformation(
                "PriceCalculatedConsumer: no StoragePath on event — skipping SignalR push (non-file flow)");
            return;
        }

        await _hub.Clients.Group($"file:{payload.StoragePath}").SendAsync(
            "PriceCalculated",
            new PriceCalculatedPayload(
                StoragePath: payload.StoragePath!,
                UnitPrice: payload.TotalUnitPrice,
                TotalPrice: payload.TotalPrice,
                Currency: payload.Currency,
                EstimatedLeadTimeDays: payload.EstimatedLeadTimeDays,
                ValidUntil: payload.ValidUntil),
            context.CancellationToken);
    }
}

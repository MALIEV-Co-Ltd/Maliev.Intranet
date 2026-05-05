using Maliev.Intranet.Bff.Clients;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PriceCalculatedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PriceCalculatedConsumer"/>.
    /// </summary>
    public PriceCalculatedConsumer(
        IHubContext<NotificationHub> hub,
        IHttpClientFactory httpClientFactory,
        ILogger<PriceCalculatedConsumer> logger)
    {
        _hub = hub;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private UploadServiceClient CreateUploadClient() =>
        new("UploadServiceClient.Consumer", _httpClientFactory);

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
            "PriceCalculatedConsumer: received event for fileId={FileId}, storagePath={StoragePath}, unitPrice={UnitPrice}",
            payload.FileId, payload.StoragePath, payload.TotalUnitPrice);

        var storagePath = await ResolveCurrentStoragePathAsync(
            CreateUploadClient(),
            payload.FileId,
            payload.StoragePath,
            context.CancellationToken);

        if (string.IsNullOrEmpty(storagePath))
        {
            _logger.LogInformation(
                "PriceCalculatedConsumer: no StoragePath or resolvable FileId on event — skipping SignalR push (non-file flow)");
            return;
        }

        var signalRPayload = new PriceCalculatedPayload(
            StoragePath: storagePath,
            UnitPrice: payload.TotalUnitPrice,
            TotalPrice: payload.TotalPrice,
            Currency: payload.Currency,
            EstimatedLeadTimeDays: payload.EstimatedLeadTimeDays,
            ValidUntil: payload.ValidUntil);

        await SendToFileGroupsAsync(payload.StoragePath, storagePath, signalRPayload, context.CancellationToken);
    }

    private async Task<string?> ResolveCurrentStoragePathAsync(
        UploadServiceClient uploadClient,
        Guid fileId,
        string? eventStoragePath,
        CancellationToken cancellationToken)
    {
        if (fileId == Guid.Empty)
        {
            return eventStoragePath;
        }

        var currentPath = await uploadClient.GetStoragePathAsync(fileId.ToString(), cancellationToken);
        if (string.IsNullOrWhiteSpace(currentPath) ||
            string.Equals(currentPath, eventStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            return eventStoragePath ?? currentPath;
        }

        _logger.LogInformation(
            "PriceCalculatedConsumer: file {FileId} moved while pricing was in flight; using current storage path {CurrentPath} instead of event path {EventPath}.",
            fileId,
            currentPath,
            eventStoragePath);
        return currentPath;
    }

    private async Task SendToFileGroupsAsync(
        string? eventStoragePath,
        string currentStoragePath,
        PriceCalculatedPayload payload,
        CancellationToken cancellationToken)
    {
        var groupPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            currentStoragePath
        };

        if (!string.IsNullOrWhiteSpace(eventStoragePath))
        {
            groupPaths.Add(eventStoragePath);
        }

        foreach (var path in groupPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            await _hub.Clients.Group($"file:{path}").SendAsync(
                "PriceCalculated",
                payload,
                cancellationToken);
        }
    }
}

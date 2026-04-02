using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="FileAnalysisFailedEvent"/> messages published by GeometryService.
/// Updates analysis status cache and notifies connected Blazor clients via SignalR so the UI unblocks.
/// </summary>
public class FileAnalysisFailedConsumer : IConsumer<FileAnalysisFailedEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly ILogger<FileAnalysisFailedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FileAnalysisFailedConsumer"/>.
    /// </summary>
    public FileAnalysisFailedConsumer(
        IHubContext<NotificationHub> hub,
        IFileAnalysisStatusService analysisStatusService,
        ILogger<FileAnalysisFailedConsumer> logger)
    {
        _hub = hub;
        _analysisStatusService = analysisStatusService;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming <see cref="FileAnalysisFailedEvent"/>, marks the file as failed in the
    /// status cache, and pushes a failure notification to the client via SignalR.
    /// </summary>
    public async Task Consume(ConsumeContext<FileAnalysisFailedEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("FileAnalysisFailedConsumer: received null message or payload");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogWarning(
            "FileAnalysisFailedConsumer: geometry analysis failed for file {FileId}, storagePath={StoragePath}, errorCode={ErrorCode}",
            payload.FileId, payload.StoragePath, payload.ErrorCode);

        await _analysisStatusService.SetAnalysisFailedAsync(
            payload.StoragePath, payload.ErrorCode, context.CancellationToken);

        await _hub.Clients.Group($"file:{payload.StoragePath}").SendAsync(
            "FileAnalysisCompleted",
            new FileAnalysisCompletedPayload(
                StoragePath: payload.StoragePath,
                UploadId: payload.FileId,
                ThumbnailUrl: null,
                HiResThumbnailUrl: null,
                Dimensions: null,
                PreviewUrls: null,
                Failed: true,
                ErrorCode: payload.ErrorCode),
            context.CancellationToken);

        _logger.LogInformation(
            "FileAnalysisFailedConsumer: pushed failure notification via SignalR for {StoragePath}",
            payload.StoragePath);
    }
}

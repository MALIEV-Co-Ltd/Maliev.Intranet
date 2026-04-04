using System.Text.Json;
using Maliev.Intranet.Bff.Clients;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Bff.Services;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// MassTransit consumer that handles <see cref="DfmAnalysisReadyEvent"/> messages published by GeometryService.
/// Persists DFM reports in the analysis cache and broadcasts results via SignalR.
/// </summary>
public class DfmAnalysisReadyConsumer : IConsumer<DfmAnalysisReadyEvent>
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly ILogger<DfmAnalysisReadyConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DfmAnalysisReadyConsumer"/>.
    /// </summary>
    public DfmAnalysisReadyConsumer(
        IHubContext<NotificationHub> hub,
        IHttpClientFactory httpClientFactory,
        IFileAnalysisStatusService analysisStatusService,
        ILogger<DfmAnalysisReadyConsumer> logger)
    {
        _hub = hub;
        _httpClientFactory = httpClientFactory;
        _analysisStatusService = analysisStatusService;
        _logger = logger;
    }

    private UploadServiceClient CreateUploadClient() =>
        new("UploadServiceClient.Consumer", _httpClientFactory);

    /// <summary>
    /// Processes an incoming <see cref="DfmAnalysisReadyEvent"/>: persists DFM reports in the
    /// BFF cache and pushes results to connected Blazor clients via SignalR.
    /// </summary>
    /// <param name="context">The MassTransit consume context.</param>
    public async Task Consume(ConsumeContext<DfmAnalysisReadyEvent> context)
    {
        if (context.Message?.Payload == null)
        {
            _logger.LogWarning("DfmAnalysisReadyConsumer: received null message or payload");
            return;
        }

        var payload = context.Message.Payload;
        _logger.LogInformation(
            "DfmAnalysisReadyConsumer: received event for file {FileId}, storagePath={StoragePath}",
            payload.FileId, payload.StoragePath);

        if (string.IsNullOrEmpty(payload.StoragePath))
        {
            _logger.LogWarning(
                "DfmAnalysisReadyConsumer: missing StoragePath for FileId={FileId} — skipping",
                payload.FileId);
            return;
        }

        var dfmReports = new
        {
            FdmReport = (object?)payload.FdmReport,
            SlaReport = (object?)payload.SlaReport,
            CncReport = (object?)payload.CncReport,
        };

        await _analysisStatusService.SetDfmReportsAsync(
            payload.StoragePath, dfmReports, context.CancellationToken);

        _logger.LogInformation(
            "DfmAnalysisReadyConsumer: cached DFM reports for storagePath={StoragePath}",
            payload.StoragePath);

        var fdmReport = DeserializeReport<FdmDfmReportPayload>(payload.FdmReport);
        var slaReport = DeserializeReport<SlaDfmReportPayload>(payload.SlaReport);
        var cncReport = DeserializeReport<CncDfmReportPayload>(payload.CncReport);

        // Sign overlay GLB paths concurrently (fire-and-forget per key)
        IReadOnlyDictionary<string, string>? overlayUrls = null;
        if (payload.OverlayPaths is { Count: > 0 })
        {
            try
            {
                var client = CreateUploadClient();
                var signTasks = payload.OverlayPaths
                    .Select(async kv =>
                    {
                        var url = await client.GetDownloadUrlByPathAsync(
                            kv.Value, context.CancellationToken);
                        return (kv.Key, Url: url);
                    });
                var signed = await Task.WhenAll(signTasks);
                overlayUrls = signed
                    .Where(x => !string.IsNullOrEmpty(x.Url))
                    .ToDictionary(x => x.Key, x => x.Url!);
                _logger.LogInformation(
                    "DfmAnalysisReadyConsumer: signed {Count} overlay URLs for storagePath={StoragePath}",
                    overlayUrls.Count, payload.StoragePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "DfmAnalysisReadyConsumer: overlay URL signing failed (non-fatal) for storagePath={StoragePath}",
                    payload.StoragePath);
            }
        }

        await _hub.Clients.Group($"file:{payload.StoragePath}").SendAsync(
            "DfmAnalysisReady",
            new DfmAnalysisReadyPayload(
                StoragePath: payload.StoragePath,
                FdmReport: fdmReport,
                SlaReport: slaReport,
                CncReport: cncReport,
                OverlayUrls: overlayUrls,
                OverlayPaths: payload.OverlayPaths),
            context.CancellationToken);
    }

    private static T? DeserializeReport<T>(object? value) where T : class
    {
        if (value is null) return null;
        if (value is T typed) return typed;
        if (value is System.Text.Json.JsonElement element)
            return JsonSerializer.Deserialize<T>(element.GetRawText());
        return null;
    }
}

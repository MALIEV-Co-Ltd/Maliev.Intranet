using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
    /// analysis cache and pushes results to connected Blazor clients via SignalR.
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

        // Merge: read existing cached reports and preserve non-null fields from prior events.
        // Without merging, a SLS/MJF/SLA_DLP event (which sets only fdm/sla report) would
        // overwrite all three fields with null, wiping data from the earlier FDM event.
        var existing = await _analysisStatusService.GetStatusAsync(payload.StoragePath, context.CancellationToken);
        object? existingFdm = null;
        object? existingSla = null;
        object? existingCnc = null;
        if (existing?.DfmReport is JsonElement existingJe && existingJe.ValueKind == JsonValueKind.Object)
        {
            if (existingJe.TryGetProperty("FdmReport", out var fp) && fp.ValueKind != JsonValueKind.Null)
                existingFdm = fp;
            if (existingJe.TryGetProperty("SlaReport", out var sp) && sp.ValueKind != JsonValueKind.Null)
                existingSla = sp;
            if (existingJe.TryGetProperty("CncReport", out var cp) && cp.ValueKind != JsonValueKind.Null)
                existingCnc = cp;
        }

        var dfmReports = new
        {
            FdmReport = payload.FdmReport != null ? (object?)payload.FdmReport : existingFdm,
            SlaReport = payload.SlaReport != null ? (object?)payload.SlaReport : existingSla,
            CncReport = payload.CncReport != null ? (object?)payload.CncReport : existingCnc,
        };

        await _analysisStatusService.SetDfmReportsAsync(
            payload.StoragePath, dfmReports, context.CancellationToken);

        _logger.LogInformation(
            "DfmAnalysisReadyConsumer: cached DFM reports for storagePath={StoragePath}",
            payload.StoragePath);

        var fdmReport = DeserializeReport<FdmDfmReportPayload>(payload.FdmReport);
        var slaReport = DeserializeReport<SlaDfmReportPayload>(payload.SlaReport);
        var cncReport = DeserializeReport<CncDfmReportPayload>(payload.CncReport);

        // Extract raw overlay paths from payload (MassTransit deserialises object? as JsonElement).
        Dictionary<string, string>? rawOverlayPaths = null;
        if (payload.OverlayPaths is JsonElement je2 && je2.ValueKind == JsonValueKind.Object)
        {
            rawOverlayPaths = je2.EnumerateObject()
                .Where(p => !string.IsNullOrEmpty(p.Value.GetString()))
                .ToDictionary(p => p.Name, p => p.Value.GetString()!);
        }
        else if (payload.OverlayPaths is IDictionary<string, object> dict2)
        {
            rawOverlayPaths = dict2
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value?.ToString()))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!.ToString()!);
        }

        // Sign all overlay URLs in parallel so the SignalR event is not delayed by 20+ serial HTTP calls.
        Dictionary<string, string>? overlayUrls = null;
        if (rawOverlayPaths is { Count: > 0 })
        {
            var signed = new ConcurrentDictionary<string, string>();
            await Task.WhenAll(rawOverlayPaths.Select(async kvp =>
            {
                try
                {
                    var url = await CreateUploadClient().GetDownloadUrlByPathAsync(
                        kvp.Value, context.CancellationToken, expirationMinutes: 10080);
                    if (!string.IsNullOrEmpty(url))
                        signed[kvp.Key] = url;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "DfmAnalysisReadyConsumer: failed to get signed URL for overlay {Key}", kvp.Key);
                }
            }));
            if (signed.Count > 0)
                overlayUrls = new Dictionary<string, string>(signed);
        }

        int? bodyCount = payload.BodyCount;
        string? nonManifoldReason = payload.NonManifoldReason;
        int? nonManifoldFaceCount = payload.NonManifoldFaceCount;

        await _hub.Clients.Group($"file:{payload.StoragePath}").SendAsync(
            "DfmAnalysisReady",
            new DfmAnalysisReadyPayload(
                StoragePath: payload.StoragePath,
                FdmReport: fdmReport,
                SlaReport: slaReport,
                CncReport: cncReport,
                OverlayUrls: overlayUrls != null
                    ? new ReadOnlyDictionary<string, string>(overlayUrls)
                    : null,
                OverlayPaths: rawOverlayPaths != null
                    ? new ReadOnlyDictionary<string, string>(rawOverlayPaths)
                    : null,
                BodyCount: bodyCount,
                NonManifoldReason: nonManifoldReason,
                NonManifoldFaceCount: nonManifoldFaceCount),
            context.CancellationToken);
    }

    private static T? DeserializeReport<T>(object? value) where T : class
    {
        if (value is null) return null;
        if (value is T typed) return typed;
        if (value is JsonElement element)
            return JsonSerializer.Deserialize<T>(element.GetRawText());
        return null;
    }
}
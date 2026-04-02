using System.Text.Json;
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
    private readonly IFileAnalysisStatusService _analysisStatusService;
    private readonly ILogger<DfmAnalysisReadyConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DfmAnalysisReadyConsumer"/>.
    /// </summary>
    public DfmAnalysisReadyConsumer(
        IHubContext<NotificationHub> hub,
        IFileAnalysisStatusService analysisStatusService,
        ILogger<DfmAnalysisReadyConsumer> logger)
    {
        _hub = hub;
        _analysisStatusService = analysisStatusService;
        _logger = logger;
    }

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

        await _hub.Clients.Group($"file:{payload.StoragePath}").SendAsync(
            "DfmAnalysisReady",
            new DfmAnalysisReadyPayload(
                StoragePath: payload.StoragePath,
                FdmReport: fdmReport,
                SlaReport: slaReport,
                CncReport: cncReport),
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

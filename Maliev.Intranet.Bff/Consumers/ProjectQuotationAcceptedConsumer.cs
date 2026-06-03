using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Projects;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// Consumes <see cref="ProjectQuotationAcceptedEvent"/> published by ProjectService.
/// Persists one <see cref="AlertNotification"/> row in PostgreSQL and immediately
/// broadcasts an <see cref="AlertSummaryDto"/> to all connected Intranet clients via SignalR.
///
/// Failure handling:
/// - DB write failure: logged; MassTransit will retry the message.
/// - SignalR broadcast failure: logged and swallowed — clients reload on next page visit.
/// - This consumer is best-effort: a project status change already succeeded before the event fires.
/// </summary>
public class ProjectQuotationAcceptedConsumer(
    IntranetDbContext db,
    IHubContext<NotificationHub> hub,
    ILogger<ProjectQuotationAcceptedConsumer> logger) : IConsumer<ProjectQuotationAcceptedEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ProjectQuotationAcceptedEvent> context)
    {
        var payload = context.Message?.Payload;
        if (payload is null)
        {
            logger.LogWarning("ProjectQuotationAcceptedConsumer: received null payload — skipping");
            return;
        }

        logger.LogInformation(
            "ProjectQuotationAcceptedConsumer: quote accepted for project {ProjectNumber} (id={ProjectId})",
            payload.ProjectNumber, payload.ProjectId);

        var now = DateTime.UtcNow;
        var processTypes = payload.Parts.Count > 0
            ? string.Join(", ", payload.Parts.Select(p => p.ProcessType).Distinct())
            : string.Empty;

        var notification = new AlertNotification
        {
            Id = Guid.NewGuid(),
            Type = "QuoteAccepted",
            ProjectId = payload.ProjectId,
            ProjectNumber = payload.ProjectNumber,
            CustomerName = string.Empty,
            PartCount = payload.Parts.Count,
            ProcessTypes = processTypes,
            OccurredAtUtc = now,
            ExpiresAtUtc = now.AddDays(7)
        };

        try
        {
            db.AlertNotifications.Add(notification);
            await db.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "ProjectQuotationAcceptedConsumer: persisted alert {AlertId} for project {ProjectNumber}",
                notification.Id, notification.ProjectNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "ProjectQuotationAcceptedConsumer: failed to persist alert for project {ProjectNumber} — rethrowing for MassTransit retry",
                payload.ProjectNumber);
            throw;
        }

        var summary = new AlertSummaryDto
        {
            Id = notification.Id,
            ProjectId = notification.ProjectId,
            ProjectNumber = notification.ProjectNumber,
            CustomerName = notification.CustomerName,
            PartCount = notification.PartCount,
            ProcessTypes = notification.ProcessTypes,
            OccurredAtUtc = notification.OccurredAtUtc
        };

        try
        {
            await hub.Clients.All.SendAsync("ReceiveAlert", summary, context.CancellationToken);

            logger.LogInformation(
                "ProjectQuotationAcceptedConsumer: broadcast ReceiveAlert for alert {AlertId}",
                notification.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "ProjectQuotationAcceptedConsumer: SignalR broadcast failed for alert {AlertId} — clients will load from DB on reconnect",
                notification.Id);
        }
    }
}

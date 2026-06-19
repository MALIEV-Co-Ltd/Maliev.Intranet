using System.Globalization;
using Maliev.Intranet.Bff.Data;
using Maliev.Intranet.Bff.Hubs;
using Maliev.Intranet.Shared.Dtos;
using Maliev.MessagingContracts.Contracts.Projects;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Maliev.Intranet.Bff.Consumers;

/// <summary>
/// Creates employee alerts for project status changes that need operational attention.
/// </summary>
public class ProjectStatusChangedAlertConsumer(
    IntranetDbContext db,
    IHubContext<NotificationHub> hub,
    ILogger<ProjectStatusChangedAlertConsumer> logger) : IConsumer<ProjectStatusChangedEvent>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ProjectStatusChangedEvent> context)
    {
        var message = context.Message;
        if (message?.Payload is null)
        {
            logger.LogWarning("ProjectStatusChangedAlertConsumer: received null payload; skipping");
            return;
        }

        var payload = message.Payload;
        if (!string.Equals(payload.NewStatus, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(
                "ProjectStatusChangedAlertConsumer: ignoring project {ProjectNumber} status {NewStatus}",
                payload.ProjectNumber,
                payload.NewStatus);
            return;
        }

        var eventDeduplicationKey = BuildEventDeduplicationKey(message, payload);
        if (await db.AlertNotifications.AnyAsync(
            notification => notification.EventDeduplicationKey == eventDeduplicationKey,
            context.CancellationToken))
        {
            logger.LogInformation(
                "ProjectStatusChangedAlertConsumer: duplicate paid-project event {EventDeduplicationKey} for project {ProjectNumber}; skipping alert",
                eventDeduplicationKey,
                payload.ProjectNumber);
            return;
        }

        var occurredAtUtc = payload.ChangedAt.UtcDateTime == default
            ? DateTime.UtcNow
            : payload.ChangedAt.UtcDateTime;

        var notification = new AlertNotification
        {
            Id = Guid.NewGuid(),
            EventDeduplicationKey = eventDeduplicationKey,
            Type = "ProjectPaid",
            ProjectId = payload.ProjectId,
            ProjectNumber = payload.ProjectNumber,
            CustomerName = string.Empty,
            PartCount = 0,
            ProcessTypes = "Payment confirmed",
            OccurredAtUtc = occurredAtUtc,
            ExpiresAtUtc = occurredAtUtc.AddDays(7)
        };

        try
        {
            db.AlertNotifications.Add(notification);
            await db.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "ProjectStatusChangedAlertConsumer: persisted paid-project alert {AlertId} for project {ProjectNumber}",
                notification.Id,
                notification.ProjectNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "ProjectStatusChangedAlertConsumer: failed to persist alert for project {ProjectNumber}",
                payload.ProjectNumber);
            throw;
        }

        var summary = new AlertSummaryDto
        {
            Id = notification.Id,
            Type = notification.Type,
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
                "ProjectStatusChangedAlertConsumer: broadcast ReceiveAlert for alert {AlertId}",
                notification.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "ProjectStatusChangedAlertConsumer: SignalR broadcast failed for alert {AlertId}; clients will load from DB on reconnect",
                notification.Id);
        }
    }

    private static string BuildEventDeduplicationKey(
        ProjectStatusChangedEvent message,
        ProjectStatusChangedEventPayload payload)
    {
        if (message.MessageId != Guid.Empty)
        {
            return $"message:{message.MessageId:N}";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"project:{payload.ProjectId:N}:status:{payload.OldStatus}:{payload.NewStatus}:{payload.ChangedAt:O}");
    }
}

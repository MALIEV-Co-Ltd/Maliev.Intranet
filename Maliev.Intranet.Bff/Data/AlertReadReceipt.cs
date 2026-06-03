namespace Maliev.Intranet.Bff.Data;

/// <summary>
/// One-row-per-(notification, employee) record that marks an alert as read.
/// Primary key is (NotificationId, EmployeeId) — composite, unique, cascade-deletes with the parent notification.
/// </summary>
public class AlertReadReceipt
{
    /// <summary>FK to the parent <see cref="AlertNotification"/>.</summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// Employee identity — matches the NameIdentifier claim used throughout the BFF.
    /// Stored as a string because the claim is a platform user UUID.
    /// </summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the employee marked the alert read.</summary>
    public DateTime ReadAtUtc { get; set; }

    /// <summary>Navigation property to the parent notification.</summary>
    public AlertNotification Notification { get; set; } = null!;
}

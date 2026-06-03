using Microsoft.EntityFrameworkCore;

namespace Maliev.Intranet.Bff.Data;

/// <summary>
/// PostgreSQL persistence context for Intranet BFF-owned application data.
/// </summary>
public class IntranetDbContext(DbContextOptions<IntranetDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Historical system health probe samples.
    /// </summary>
    public DbSet<HealthCheckSample> HealthCheckSamples => Set<HealthCheckSample>();

    /// <summary>
    /// Quote-accepted alert notifications broadcast to all employees.
    /// </summary>
    public DbSet<AlertNotification> AlertNotifications => Set<AlertNotification>();

    /// <summary>
    /// Per-employee read receipts for alert notifications.
    /// </summary>
    public DbSet<AlertReadReceipt> AlertReadReceipts => Set<AlertReadReceipt>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── HealthCheckSample ─────────────────────────────────────────────────
        var sample = modelBuilder.Entity<HealthCheckSample>();
        sample.ToTable("health_check_samples");
        sample.HasKey(x => x.Id);
        sample.Property(x => x.Id).HasColumnName("id");
        sample.Property(x => x.SampledAtUtc).HasColumnName("sampled_at_utc").IsRequired();
        sample.Property(x => x.ServiceName).HasColumnName("service_name").HasMaxLength(128).IsRequired();
        sample.Property(x => x.DomainGroup).HasColumnName("domain_group").HasMaxLength(64).IsRequired();
        sample.Property(x => x.RoutePrefix).HasColumnName("route_prefix").HasMaxLength(128).IsRequired();
        sample.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        sample.Property(x => x.IsCritical).HasColumnName("is_critical").IsRequired();
        sample.Property(x => x.LivenessPath).HasColumnName("liveness_path").HasMaxLength(256).IsRequired();
        sample.Property(x => x.ReadinessPath).HasColumnName("readiness_path").HasMaxLength(256).IsRequired();
        sample.Property(x => x.LivenessResponseTimeMs).HasColumnName("liveness_response_time_ms").IsRequired();
        sample.Property(x => x.ReadinessResponseTimeMs).HasColumnName("readiness_response_time_ms").IsRequired();
        sample.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
        sample.Property(x => x.ErrorBody).HasColumnName("error_body").HasMaxLength(2000);
        sample.HasIndex(x => x.SampledAtUtc);
        sample.HasIndex(x => new { x.ServiceName, x.SampledAtUtc });
        sample.HasIndex(x => new { x.DomainGroup, x.SampledAtUtc });

        // ── AlertNotification ────────────────────────────────────────────────
        var alert = modelBuilder.Entity<AlertNotification>();
        alert.ToTable("alert_notifications");
        alert.HasKey(x => x.Id);
        alert.Property(x => x.Id).HasColumnName("id");
        alert.Property(x => x.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        alert.Property(x => x.ProjectId).HasColumnName("project_id").IsRequired();
        alert.Property(x => x.ProjectNumber).HasColumnName("project_number").HasMaxLength(32).IsRequired();
        alert.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(256).IsRequired();
        alert.Property(x => x.PartCount).HasColumnName("part_count").IsRequired();
        alert.Property(x => x.ProcessTypes).HasColumnName("process_types").HasMaxLength(256).IsRequired();
        alert.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        alert.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        alert.HasIndex(x => x.ExpiresAtUtc);

        // ── AlertReadReceipt ─────────────────────────────────────────────────
        var receipt = modelBuilder.Entity<AlertReadReceipt>();
        receipt.ToTable("alert_read_receipts");
        receipt.HasKey(x => new { x.NotificationId, x.EmployeeId });
        receipt.Property(x => x.NotificationId).HasColumnName("notification_id");
        receipt.Property(x => x.EmployeeId).HasColumnName("employee_id").HasMaxLength(128);
        receipt.Property(x => x.ReadAtUtc).HasColumnName("read_at_utc").IsRequired();
        receipt.HasOne(x => x.Notification)
            .WithMany()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

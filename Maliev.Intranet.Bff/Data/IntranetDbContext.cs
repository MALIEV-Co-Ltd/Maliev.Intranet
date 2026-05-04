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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}

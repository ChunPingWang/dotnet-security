using Microsoft.EntityFrameworkCore;
using RbacSso.Audit.Domain;

namespace RbacSso.AuditService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework DbContext for Audit Service.
/// </summary>
public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .IsRequired();

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_audit_logs_timestamp");

            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("ix_audit_logs_event_type");

            entity.Property(e => e.AggregateType)
                .HasColumnName("aggregate_type")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.AggregateId)
                .HasColumnName("aggregate_id")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => new { e.AggregateType, e.AggregateId })
                .HasDatabaseName("ix_audit_logs_aggregate");

            entity.Property(e => e.Username)
                .HasColumnName("username")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(e => e.Username)
                .HasDatabaseName("ix_audit_logs_username");

            entity.Property(e => e.ServiceName)
                .HasColumnName("service_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Action)
                .HasColumnName("action")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.Result)
                .HasColumnName("result")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(2000);

            entity.Property(e => e.ClientIp)
                .HasColumnName("client_ip")
                .HasMaxLength(45); // IPv6 max length

            entity.Property(e => e.CorrelationId)
                .HasColumnName("correlation_id")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("ix_audit_logs_correlation_id");

            entity.Property(e => e.PayloadTruncated)
                .HasColumnName("payload_truncated")
                .IsRequired();

            // Composite index for common queries
            entity.HasIndex(e => new { e.Timestamp, e.EventType })
                .HasDatabaseName("ix_audit_logs_timestamp_event_type");

            entity.HasIndex(e => new { e.Username, e.Timestamp })
                .HasDatabaseName("ix_audit_logs_username_timestamp");
        });
    }
}

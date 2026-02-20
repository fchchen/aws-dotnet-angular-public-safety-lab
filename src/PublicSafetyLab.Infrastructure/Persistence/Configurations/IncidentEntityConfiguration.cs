using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicSafetyLab.Infrastructure.Persistence.Entities;

namespace PublicSafetyLab.Infrastructure.Persistence.Configurations;

public sealed class IncidentEntityConfiguration : IEntityTypeConfiguration<IncidentEntity>
{
    public void Configure(EntityTypeBuilder<IncidentEntity> builder)
    {
        builder.ToTable("incidents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasColumnName("priority")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Location)
            .HasColumnName("location")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ReportedAt)
            .HasColumnName("reported_at")
            .IsRequired();

        builder.Property(x => x.QueuedAt)
            .HasColumnName("queued_at");

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1024);

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt })
            .HasDatabaseName("ix_incidents_tenant_status_created_at")
            .IsDescending(false, false, true);

        builder.HasIndex(x => new { x.TenantId, x.CreatedAt })
            .HasDatabaseName("ix_incidents_tenant_created_at")
            .IsDescending(false, true);
    }
}

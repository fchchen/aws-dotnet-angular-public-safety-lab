using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicSafetyLab.Infrastructure.Persistence.Entities;

namespace PublicSafetyLab.Infrastructure.Persistence.Configurations;

public sealed class EvidenceItemEntityConfiguration : IEntityTypeConfiguration<EvidenceItemEntity>
{
    public void Configure(EntityTypeBuilder<EvidenceItemEntity> builder)
    {
        builder.ToTable("evidence_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.IncidentId)
            .HasColumnName("incident_id")
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.UploadedAt)
            .HasColumnName("uploaded_at")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasOne(x => x.Incident)
            .WithMany(x => x.EvidenceItems)
            .HasForeignKey(x => x.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.IncidentId)
            .HasDatabaseName("ix_evidence_items_incident_id");
    }
}

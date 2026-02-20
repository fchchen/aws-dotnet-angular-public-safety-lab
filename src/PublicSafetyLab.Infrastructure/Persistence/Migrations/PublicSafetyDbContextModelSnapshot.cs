using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PublicSafetyLab.Infrastructure.Persistence;

#nullable disable

namespace PublicSafetyLab.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PublicSafetyDbContext))]
partial class PublicSafetyDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0");

        modelBuilder.Entity("PublicSafetyLab.Infrastructure.Persistence.Entities.EvidenceItemEntity", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<string>("FileName")
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasColumnType("character varying(512)")
                    .HasColumnName("file_name");

                b.Property<Guid>("IncidentId")
                    .HasColumnType("uuid")
                    .HasColumnName("incident_id");

                b.Property<string>("ObjectKey")
                    .IsRequired()
                    .HasMaxLength(1024)
                    .HasColumnType("character varying(1024)")
                    .HasColumnName("object_key");

                b.Property<int>("SortOrder")
                    .HasColumnType("integer")
                    .HasColumnName("sort_order");

                b.Property<DateTimeOffset>("UploadedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("uploaded_at");

                b.HasKey("Id");

                b.HasIndex("IncidentId")
                    .HasDatabaseName("ix_evidence_items_incident_id");

                b.ToTable("evidence_items", (string)null);
            });

        modelBuilder.Entity("PublicSafetyLab.Infrastructure.Persistence.Entities.IncidentEntity", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTimeOffset>("CreatedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at");

                b.Property<string>("Description")
                    .IsRequired()
                    .HasMaxLength(4000)
                    .HasColumnType("character varying(4000)")
                    .HasColumnName("description");

                b.Property<string>("FailureReason")
                    .HasMaxLength(1024)
                    .HasColumnType("character varying(1024)")
                    .HasColumnName("failure_reason");

                b.Property<string>("Location")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)")
                    .HasColumnName("location");

                b.Property<string>("Priority")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)")
                    .HasColumnName("priority");

                b.Property<DateTimeOffset?>("ProcessedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("processed_at");

                b.Property<DateTimeOffset?>("QueuedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("queued_at");

                b.Property<DateTimeOffset>("ReportedAt")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("reported_at");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)")
                    .HasColumnName("status");

                b.Property<string>("TenantId")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)")
                    .HasColumnName("tenant_id");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)")
                    .HasColumnName("title");

                b.Property<int>("Version")
                    .HasColumnType("integer")
                    .IsConcurrencyToken()
                    .HasColumnName("version");

                b.HasKey("Id");

                b.HasIndex("TenantId", "CreatedAt")
                    .HasDatabaseName("ix_incidents_tenant_created_at")
                    .IsDescending(false, true);

                b.HasIndex("TenantId", "Status", "CreatedAt")
                    .HasDatabaseName("ix_incidents_tenant_status_created_at")
                    .IsDescending(false, false, true);

                b.ToTable("incidents", (string)null);
            });

        modelBuilder.Entity("PublicSafetyLab.Infrastructure.Persistence.Entities.EvidenceItemEntity", b =>
            {
                b.HasOne("PublicSafetyLab.Infrastructure.Persistence.Entities.IncidentEntity", "Incident")
                    .WithMany("EvidenceItems")
                    .HasForeignKey("IncidentId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Incident");
            });

        modelBuilder.Entity("PublicSafetyLab.Infrastructure.Persistence.Entities.IncidentEntity", b =>
            {
                b.Navigation("EvidenceItems");
            });
#pragma warning restore 612, 618
    }
}

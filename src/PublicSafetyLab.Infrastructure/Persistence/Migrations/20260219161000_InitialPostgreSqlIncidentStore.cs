using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PublicSafetyLab.Infrastructure.Persistence;

#nullable disable

namespace PublicSafetyLab.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PublicSafetyDbContext))]
[Migration("20260219161000_InitialPostgreSqlIncidentStore")]
public partial class InitialPostgreSqlIncidentStore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "incidents",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                reported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                failure_reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_incidents", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "evidence_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                object_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_evidence_items", x => x.id);
                table.ForeignKey(
                    name: "fk_evidence_items_incidents_incident_id",
                    column: x => x.incident_id,
                    principalTable: "incidents",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_evidence_items_incident_id",
            table: "evidence_items",
            column: "incident_id");

        migrationBuilder.CreateIndex(
            name: "ix_incidents_tenant_created_at",
            table: "incidents",
            columns: new[] { "tenant_id", "created_at" },
            descending: new[] { false, true });

        migrationBuilder.CreateIndex(
            name: "ix_incidents_tenant_status_created_at",
            table: "incidents",
            columns: new[] { "tenant_id", "status", "created_at" },
            descending: new[] { false, false, true });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "evidence_items");

        migrationBuilder.DropTable(
            name: "incidents");
    }
}

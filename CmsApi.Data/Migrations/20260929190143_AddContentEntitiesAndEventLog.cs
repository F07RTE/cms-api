using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CmsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContentEntitiesAndEventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_entities",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    last_event_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    is_disabled_by_admin = table.Column<bool>(type: "boolean", nullable: false),
                    disabled_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    disabled_by = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_entities", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "event_log",
                columns: table => new
                {
                    id = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    batch_id = table.Column<long>(type: "bigint", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: true),
                    event_timestamp = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    raw_event = table.Column<string>(type: "text", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_log_inbox_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "inbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_content_entities_last_event_at_id",
                table: "content_entities",
                columns: new[] { "last_event_at", "id" },
                descending: new[] { true, false }
            );

            migrationBuilder.CreateIndex(
                name: "ix_content_entities_visible_last_event_at_id",
                table: "content_entities",
                columns: new[] { "last_event_at", "id" },
                descending: new[] { true, false },
                filter: "is_published AND NOT is_disabled_by_admin"
            );

            migrationBuilder.CreateIndex(
                name: "ix_event_log_batch_id",
                table: "event_log",
                column: "batch_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_event_log_entity_id_id",
                table: "event_log",
                columns: new[] { "entity_id", "id" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "content_entities");

            migrationBuilder.DropTable(name: "event_log");
        }
    }
}

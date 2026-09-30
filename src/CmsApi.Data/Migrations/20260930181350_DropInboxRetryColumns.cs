using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropInboxRetryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Processing leaves the status enum; a row left in it could no longer be read.
            migrationBuilder.Sql(
                "UPDATE inbox SET status = 'Pending' WHERE status = 'Processing';"
            );

            migrationBuilder.DropIndex(name: "ix_inbox_next_attempt_at_id", table: "inbox");

            migrationBuilder.DropColumn(name: "attempts", table: "inbox");

            migrationBuilder.DropColumn(name: "next_attempt_at", table: "inbox");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempts",
                table: "inbox",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_attempt_at",
                table: "inbox",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(
                    new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    new TimeSpan(0, 0, 0, 0, 0)
                )
            );

            migrationBuilder.CreateIndex(
                name: "ix_inbox_next_attempt_at_id",
                table: "inbox",
                columns: new[] { "next_attempt_at", "id" },
                filter: "status = 'Pending'"
            );
        }
    }
}

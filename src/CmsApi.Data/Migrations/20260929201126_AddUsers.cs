using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CmsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                }
            );

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "password_hash", "role", "username" },
                values: new object[,]
                {
                    {
                        new Guid("5b1f0c9e-2d4a-4e7b-8c3f-1a6d9e2b7c40"),
                        "$argon2id$v=19$m=19456,t=2,p=1$OLXIgDfhEDkEgUiWOhjZ+A$WlaQcUtsEyNRPgpTN0Zaf6DOtE8P+KFOGwR9CHU6smA",
                        "Admin",
                        "admin",
                    },
                    {
                        new Guid("8e3a6d21-7c5b-4f90-a1e4-3d2c8b6f5a19"),
                        "$argon2id$v=19$m=19456,t=2,p=1$SKxVoHRqiIBsIR2YouAfdA$C5+Yz6MqMxWWNi7Vu7Ay6YUn+lMf3vSbD5TBkZwGkk0",
                        "User",
                        "reader",
                    },
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "users");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDevUserPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("5b1f0c9e-2d4a-4e7b-8c3f-1a6d9e2b7c40"),
                column: "password_hash",
                value: "$argon2id$v=19$m=19456,t=2,p=1$Fv52v2hZpdZghTdBrt+4dg$7ecPyqGcRQUZKWrABnd8aECTIKVWlkjUBU3FQSZAdpY"
            );

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("8e3a6d21-7c5b-4f90-a1e4-3d2c8b6f5a19"),
                column: "password_hash",
                value: "$argon2id$v=19$m=19456,t=2,p=1$skZWs2Vzjq8tXNNLb9nqiA$rWK4wkmROwm4dyYDeYdm/CqARAXhmJcIfao5cBrOfao"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("5b1f0c9e-2d4a-4e7b-8c3f-1a6d9e2b7c40"),
                column: "password_hash",
                value: "$argon2id$v=19$m=19456,t=2,p=1$OLXIgDfhEDkEgUiWOhjZ+A$WlaQcUtsEyNRPgpTN0Zaf6DOtE8P+KFOGwR9CHU6smA"
            );

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("8e3a6d21-7c5b-4f90-a1e4-3d2c8b6f5a19"),
                column: "password_hash",
                value: "$argon2id$v=19$m=19456,t=2,p=1$SKxVoHRqiIBsIR2YouAfdA$C5+Yz6MqMxWWNi7Vu7Ay6YUn+lMf3vSbD5TBkZwGkk0"
            );
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gcmac.Registration.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberAccounts",
                columns: table => new
                {
                    MemberNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Organisation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PinCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    MobileVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberAccounts", x => x.MemberNo);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberAccounts_Email",
                table: "MemberAccounts",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberAccounts");
        }
    }
}

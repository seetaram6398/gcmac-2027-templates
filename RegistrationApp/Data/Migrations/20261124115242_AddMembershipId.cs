using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gcmac.Registration.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MembershipId",
                table: "Registrations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_MembershipId",
                table: "Registrations",
                column: "MembershipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_MembershipId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "MembershipId",
                table: "Registrations");
        }
    }
}

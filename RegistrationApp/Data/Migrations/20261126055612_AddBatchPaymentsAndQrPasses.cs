using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gcmac.Registration.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchPaymentsAndQrPasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdditionalDelegate",
                table: "Registrations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentBatchId",
                table: "Registrations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BatchId",
                table: "PaymentTransactions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "PaymentTransactions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegistrationIdsCsv",
                table: "PaymentTransactions",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HtmlBody",
                table: "Notifications",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_PaymentBatchId",
                table: "Registrations",
                column: "PaymentBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_BatchId",
                table: "PaymentTransactions",
                column: "BatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_PaymentBatchId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_BatchId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "IsAdditionalDelegate",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "PaymentBatchId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "RegistrationIdsCsv",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "HtmlBody",
                table: "Notifications");
        }
    }
}

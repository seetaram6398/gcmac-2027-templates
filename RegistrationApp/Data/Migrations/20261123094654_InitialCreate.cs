using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gcmac.Registration.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    MerchantTxnRefNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RegistrationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GatewayTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.MerchantTxnRefNo);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AccountEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PinCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganisationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganisationAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MembershipNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentRegistrationNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccupancyType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GstDetailsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Gstin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SponsorMemberNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddedByAccountEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GstRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    GstAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsEarlyBird = table.Column<bool>(type: "bit", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status_NextAttemptAt",
                table: "Notifications",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_RegistrationId",
                table: "PaymentTransactions",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Status",
                table: "PaymentTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_AccountEmail",
                table: "Registrations",
                column: "AccountEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_PaymentStatus",
                table: "Registrations",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_SponsorMemberNo",
                table: "Registrations",
                column: "SponsorMemberNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "Registrations");
        }
    }
}

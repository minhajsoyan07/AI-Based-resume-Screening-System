using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIResumeScreeningSystem.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionUpdateV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreditsUsed",
                table: "CreditTransactions",
                newName: "CreditsAmount");

            migrationBuilder.AddColumn<bool>(
                name: "AdminVerified",
                table: "CreditTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountBDT",
                table: "CreditTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "CreditTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionReference",
                table: "CreditTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminEarnings",
                columns: table => new
                {
                    EarningID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    AmountBDT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminEarnings", x => x.EarningID);
                    table.ForeignKey(
                        name: "FK_AdminEarnings_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationTokens",
                columns: table => new
                {
                    TokenID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationTokens", x => x.TokenID);
                    table.ForeignKey(
                        name: "FK_EmailVerificationTokens_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminEarnings_UserID",
                table: "AdminEarnings",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_UserID",
                table: "EmailVerificationTokens",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminEarnings");

            migrationBuilder.DropTable(
                name: "EmailVerificationTokens");

            migrationBuilder.DropColumn(
                name: "AdminVerified",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "AmountBDT",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionReference",
                table: "CreditTransactions");

            migrationBuilder.RenameColumn(
                name: "CreditsAmount",
                table: "CreditTransactions",
                newName: "CreditsUsed");
        }
    }
}

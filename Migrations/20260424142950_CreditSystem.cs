using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIResumeScreeningSystem.Migrations
{
    /// <inheritdoc />
    public partial class CreditSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "Recruiters");

            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PremiumVisibleFrom",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "IsPriorityApplication",
                table: "Applications");

            migrationBuilder.AddColumn<int>(
                name: "Credits",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstLogin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CreditTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CreditsUsed = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransactions", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 3,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 4,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 5,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 6,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 7,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 8,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 9,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 10,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 11,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 12,
                columns: new[] { "Credits", "IsFirstLogin" },
                values: new object[] { 0, true });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_UserID",
                table: "CreditTransactions",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "Credits",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsFirstLogin",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionTier",
                table: "Recruiters",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PremiumVisibleFrom",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriorityApplication",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 1,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 2,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 3,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 4,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 5,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 6,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 7,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 8,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 9,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 10,
                columns: new[] { "IsPremium", "PremiumVisibleFrom" },
                values: new object[] { false, null });

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 1,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 2,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 3,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 4,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 5,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 6,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 7,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 8,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 9,
                column: "SubscriptionTier",
                value: "Free");

            migrationBuilder.UpdateData(
                table: "Recruiters",
                keyColumn: "RecruiterID",
                keyValue: 10,
                column: "SubscriptionTier",
                value: "Free");
        }
    }
}

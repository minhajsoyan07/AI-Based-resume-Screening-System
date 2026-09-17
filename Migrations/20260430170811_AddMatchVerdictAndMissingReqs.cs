using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIResumeScreeningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchVerdictAndMissingReqs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchVerdict",
                table: "Applications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingRequirements",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchVerdict",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "MissingRequirements",
                table: "Applications");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$g5uFKa6eTRmnfEwTsr1h/.P4AzIoWDBYSFErzfyRhIN.N0cksZIvO");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$Gaq.bbKqKsE13nRQlB2hPeMmCZzYgwKq85Oa2AkhLIm/MklFrF.zC");
        }
    }
}

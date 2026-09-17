using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AIResumeScreeningSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJobSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 1,
                columns: new[] { "AttachmentPath", "JobDescription", "JobLocation", "JobType", "MinimumExperience", "SalaryMax", "SalaryMin" },
                values: new object[] { "/uploads/jobs/sample_circular.pdf", "Exciting opportunity for a Senior .NET Developer at TechCorp.", "Dhaka", "Contract", 3, 88000m, 55000m });

            migrationBuilder.InsertData(
                table: "Jobs",
                columns: new[] { "JobID", "ApplicationDeadline", "AttachmentPath", "CompanyID", "CreatedDate", "EducationLevel", "IsPremium", "JobCategoryID", "JobDescription", "JobLocation", "JobSector", "JobStatus", "JobTitle", "JobType", "MinimumExperience", "PremiumVisibleFrom", "RecruiterID", "RequiredSkills", "SalaryMax", "SalaryMin", "UpdatedDate" },
                values: new object[,]
                {
                    { 2, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a Frontend Engineer (React) at TechCorp.", "Chittagong", "Private", "Open", "Frontend Engineer (React)", "Full Time", 4, null, 2, "C#, ASP.NET Core, SQL Server", 96000m, 60000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a SQL Database Admin at TechCorp.", "Sylhet", "Private", "Open", "SQL Database Admin", "Contract", 5, null, 3, "C#, ASP.NET Core, SQL Server", 104000m, 65000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a DevOps Specialist at TechCorp.", "Remote", "Private", "Open", "DevOps Specialist", "Full Time", 6, null, 4, "C#, ASP.NET Core, SQL Server", 112000m, 70000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a Product Manager at TechCorp.", "Dhaka", "Private", "Open", "Product Manager", "Contract", 2, null, 5, "C#, ASP.NET Core, SQL Server", 120000m, 75000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a UI/UX Designer at TechCorp.", "Dhaka", "Private", "Open", "UI/UX Designer", "Full Time", 3, null, 6, "C#, ASP.NET Core, SQL Server", 128000m, 80000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 7, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a QA Automation Engineer at TechCorp.", "Remote", "Private", "Open", "QA Automation Engineer", "Contract", 4, null, 7, "C#, ASP.NET Core, SQL Server", 136000m, 85000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 8, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a Machine Learning Engineer at TechCorp.", "Dhaka", "Private", "Open", "Machine Learning Engineer", "Full Time", 5, null, 8, "C#, ASP.NET Core, SQL Server", 144000m, 90000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 9, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a Cloud Architect at TechCorp.", "Chittagong", "Private", "Open", "Cloud Architect", "Contract", 6, null, 9, "C#, ASP.NET Core, SQL Server", 152000m, 95000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 10, null, "/uploads/jobs/sample_circular.pdf", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "Exciting opportunity for a Full Stack Developer at TechCorp.", "Remote", "Private", "Open", "Full Stack Developer", "Full Time", 2, null, 10, "C#, ASP.NET Core, SQL Server", 160000m, 100000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 10);

            migrationBuilder.UpdateData(
                table: "Jobs",
                keyColumn: "JobID",
                keyValue: 1,
                columns: new[] { "AttachmentPath", "JobDescription", "JobLocation", "JobType", "MinimumExperience", "SalaryMax", "SalaryMin" },
                values: new object[] { null, "We are looking for a Senior .NET Developer.", "Dhaka, Bangladesh", "Full Time", 5, 120000m, 80000m });
        }
    }
}

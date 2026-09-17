using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AIResumeScreeningSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialMinimalistMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    CompanyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyWebsite = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CompanyLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanySize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IndustryType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompanyNameBangla = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OrganizationCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tagline = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoverPhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignaturePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstablishmentYear = table.Column<int>(type: "int", nullable: true),
                    OrganizationEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Vision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mission = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Division = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Thana = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FullAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FullAddressBangla = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LandPhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.CompanyID);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    SkillID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SkillCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.SkillID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProfilePicturePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "Applicants",
                columns: table => new
                {
                    ApplicantID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CurrentLocation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LinkedInURL = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PortfolioURL = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false),
                    HighestEducation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentJobTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProfileSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Certifications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Languages = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Projects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Achievements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResumeFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProfileCompletionPercent = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProfileVisibility = table.Column<bool>(type: "bit", nullable: false),
                    DataSharingConsent = table.Column<bool>(type: "bit", nullable: false),
                    EmailFrequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InAppNotifications = table.Column<bool>(type: "bit", nullable: false),
                    SearchStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicants", x => x.ApplicantID);
                    table.ForeignKey(
                        name: "FK_Applicants_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recruiters",
                columns: table => new
                {
                    RecruiterID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CompanyID = table.Column<int>(type: "int", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyWebsite = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CompanyLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanySize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IndustryType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecruiterStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubscriptionTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruiters", x => x.RecruiterID);
                    table.ForeignKey(
                        name: "FK_Recruiters_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Recruiters_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantSkills",
                columns: table => new
                {
                    ApplicantSkillID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantID = table.Column<int>(type: "int", nullable: false),
                    SkillID = table.Column<int>(type: "int", nullable: false),
                    SkillLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantSkills", x => x.ApplicantSkillID);
                    table.ForeignKey(
                        name: "FK_ApplicantSkills_Applicants_ApplicantID",
                        column: x => x.ApplicantID,
                        principalTable: "Applicants",
                        principalColumn: "ApplicantID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicantSkills_Skills_SkillID",
                        column: x => x.SkillID,
                        principalTable: "Skills",
                        principalColumn: "SkillID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    JobID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecruiterID = table.Column<int>(type: "int", nullable: false),
                    CompanyID = table.Column<int>(type: "int", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredSkills = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinimumExperience = table.Column<int>(type: "int", nullable: false),
                    EducationLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SalaryMin = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalaryMax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobSector = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JobCategoryID = table.Column<int>(type: "int", nullable: true),
                    ApplicationDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPremium = table.Column<bool>(type: "bit", nullable: false),
                    PremiumVisibleFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.JobID);
                    table.ForeignKey(
                        name: "FK_Jobs_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "CompanyID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Jobs_Recruiters_RecruiterID",
                        column: x => x.RecruiterID,
                        principalTable: "Recruiters",
                        principalColumn: "RecruiterID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ApplicationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantID = table.Column<int>(type: "int", nullable: true),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    ResumeFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplicationTrackingID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MatchScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SkillScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ExperienceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EducationScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ApplicationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AppliedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPriorityApplication = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ApplicationID);
                    table.ForeignKey(
                        name: "FK_Applications_Applicants_ApplicantID",
                        column: x => x.ApplicantID,
                        principalTable: "Applicants",
                        principalColumn: "ApplicantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Applications_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobSkills",
                columns: table => new
                {
                    JobSkillID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobID = table.Column<int>(type: "int", nullable: false),
                    SkillID = table.Column<int>(type: "int", nullable: false),
                    ImportanceLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSkills", x => x.JobSkillID);
                    table.ForeignKey(
                        name: "FK_JobSkills_Jobs_JobID",
                        column: x => x.JobID,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobSkills_Skills_SkillID",
                        column: x => x.SkillID,
                        principalTable: "Skills",
                        principalColumn: "SkillID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Interviews",
                columns: table => new
                {
                    InterviewID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationID = table.Column<int>(type: "int", nullable: false),
                    InterviewRound = table.Column<int>(type: "int", nullable: false),
                    InterviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InterviewTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    InterviewType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MeetingLink = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InterviewLocation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InterviewStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InterviewResult = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviews", x => x.InterviewID);
                    table.ForeignKey(
                        name: "FK_Interviews_Applications_ApplicationID",
                        column: x => x.ApplicationID,
                        principalTable: "Applications",
                        principalColumn: "ApplicationID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "CompanyID", "AboutOrganization", "CompanyLocation", "CompanyName", "CompanyNameBangla", "CompanySize", "CompanyStatus", "CompanyWebsite", "CoverPhotoPath", "CreatedDate", "Description", "District", "Division", "EstablishmentYear", "FullAddress", "FullAddressBangla", "IndustryType", "LandPhoneNumber", "LogoPath", "Mission", "MobileNumber", "OrganizationCategory", "OrganizationEmail", "PostalCode", "SignaturePath", "Tagline", "Tags", "Thana", "Vision" },
                values: new object[] { 1, null, "Dhaka, Bangladesh", "TechCorp Solutions", null, "201-500", "Active", "https://techcorp.example.com", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, null, null, null, "Information Technology", null, null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "SkillID", "CreatedDate", "SkillCategory", "SkillName" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Programming", "C#" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Framework", "ASP.NET Core" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Database", "SQL Server" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "AccountStatus", "CreatedDate", "Email", "EmailVerified", "FullName", "LastLoginDate", "PasswordHash", "PhoneNumber", "ProfilePicturePath", "Role", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@janala.com", true, "System Admin", null, "$2a$11$g5uFKa6eTRmnfEwTsr1h/.P4AzIoWDBYSFErzfyRhIN.N0cksZIvO", null, null, "Admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "applicant@janala.com", true, "Main Applicant", null, "$2a$11$Gaq.bbKqKsE13nRQlB2hPeMmCZzYgwKq85Oa2AkhLIm/MklFrF.zC", null, null, "Applicant", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter1@janala.com", true, "Recruiter 1", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter2@janala.com", true, "Recruiter 2", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter3@janala.com", true, "Recruiter 3", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter4@janala.com", true, "Recruiter 4", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 7, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter5@janala.com", true, "Recruiter 5", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 8, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter6@janala.com", true, "Recruiter 6", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 9, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter7@janala.com", true, "Recruiter 7", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 10, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter8@janala.com", true, "Recruiter 8", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 11, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter9@janala.com", true, "Recruiter 9", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 12, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "recruiter10@janala.com", true, "Recruiter 10", null, "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", null, null, "Recruiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Applicants",
                columns: new[] { "ApplicantID", "Achievements", "Address", "Certifications", "CreatedDate", "CurrentJobTitle", "CurrentLocation", "DataSharingConsent", "DateOfBirth", "EmailFrequency", "Gender", "HighestEducation", "InAppNotifications", "Languages", "LastActivityDate", "LastLoginDate", "LinkedInURL", "PortfolioURL", "ProfileCompletionPercent", "ProfileSummary", "ProfileVisibility", "Projects", "ResumeFilePath", "SearchStatus", "UserID", "YearsOfExperience" },
                values: new object[] { 1, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, true, null, "Daily", null, null, true, null, null, null, null, null, 0, null, true, null, null, "active", 2, 3 });

            migrationBuilder.InsertData(
                table: "Recruiters",
                columns: new[] { "RecruiterID", "CompanyID", "CompanyLocation", "CompanyName", "CompanySize", "CompanyWebsite", "CreatedDate", "Designation", "IndustryType", "RecruiterStatus", "SubscriptionTier", "UserID" },
                values: new object[,]
                {
                    { 1, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 1", null, "Active", "Free", 3 },
                    { 2, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 2", null, "Active", "Free", 4 },
                    { 3, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 3", null, "Active", "Free", 5 },
                    { 4, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 4", null, "Active", "Free", 6 },
                    { 5, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 5", null, "Active", "Free", 7 },
                    { 6, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 6", null, "Active", "Free", 8 },
                    { 7, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 7", null, "Active", "Free", 9 },
                    { 8, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 8", null, "Active", "Free", 10 },
                    { 9, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 9", null, "Active", "Free", 11 },
                    { 10, 1, null, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Technical Recruiter 10", null, "Active", "Free", 12 }
                });

            migrationBuilder.InsertData(
                table: "Jobs",
                columns: new[] { "JobID", "ApplicationDeadline", "AttachmentPath", "CompanyID", "CreatedDate", "EducationLevel", "IsPremium", "JobCategoryID", "JobDescription", "JobLocation", "JobSector", "JobStatus", "JobTitle", "JobType", "MinimumExperience", "PremiumVisibleFrom", "RecruiterID", "RequiredSkills", "SalaryMax", "SalaryMin", "UpdatedDate" },
                values: new object[] { 1, null, null, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "BSc", false, null, "We are looking for a Senior .NET Developer.", "Dhaka, Bangladesh", "Private", "Open", "Senior .NET Developer", "Full Time", 5, null, 1, "C#, ASP.NET Core, SQL Server", 120000m, 80000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_UserID",
                table: "Applicants",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantSkills_ApplicantID",
                table: "ApplicantSkills",
                column: "ApplicantID");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantSkills_SkillID",
                table: "ApplicantSkills",
                column: "SkillID");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicantID",
                table: "Applications",
                column: "ApplicantID");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicationTrackingID",
                table: "Applications",
                column: "ApplicationTrackingID",
                unique: true,
                filter: "[ApplicationTrackingID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_JobID",
                table: "Applications",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies",
                column: "CompanyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_ApplicationID",
                table: "Interviews",
                column: "ApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CompanyID",
                table: "Jobs",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RecruiterID",
                table: "Jobs",
                column: "RecruiterID");

            migrationBuilder.CreateIndex(
                name: "IX_JobSkills_JobID",
                table: "JobSkills",
                column: "JobID");

            migrationBuilder.CreateIndex(
                name: "IX_JobSkills_SkillID",
                table: "JobSkills",
                column: "SkillID");

            migrationBuilder.CreateIndex(
                name: "IX_Recruiters_CompanyID",
                table: "Recruiters",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Recruiters_UserID",
                table: "Recruiters",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicantSkills");

            migrationBuilder.DropTable(
                name: "Interviews");

            migrationBuilder.DropTable(
                name: "JobSkills");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "Applicants");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "Recruiters");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;

namespace AIResumeScreeningSystem.Services
{
    /// <summary>
    /// Provides direct access to all _janala stored procedures via EF Core.
    /// Use this alongside existing EF services — not as a replacement.
    /// Naming: ModuleName_Entity_Action_janala
    /// </summary>
    public class StoredProcedureService
    {
        private readonly ApplicationDbContext _db;

        public StoredProcedureService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ──────────────────────────────────────────────
        // HELPERS
        // ──────────────────────────────────────────────

        /// <summary>Execute a stored procedure that returns no result set.</summary>
        private async Task ExecAsync(string sql, params SqlParameter[] parameters)
        {
            await _db.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        /// <summary>Build a SQL EXEC string for a procedure with named params.</summary>
        private static string BuildExec(string procName, IEnumerable<SqlParameter> parameters)
        {
            var paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterName}={p.ParameterName}"));
            return $"EXEC {procName} {paramList}";
        }

        // ──────────────────────────────────────────────
        // USER MODULE
        // ──────────────────────────────────────────────

        /// <summary>User_UpdateLastLogin_janala — update login timestamp.</summary>
        public async Task User_UpdateLastLoginAsync(int userId)
        {
            var p = new SqlParameter("@UserID", userId);
            await ExecAsync("EXEC User_UpdateLastLogin_janala @UserID", p);
        }

        /// <summary>User_VerifyEmail_janala — mark email as verified.</summary>
        public async Task User_VerifyEmailAsync(int userId)
        {
            var p = new SqlParameter("@UserID", userId);
            await ExecAsync("EXEC User_VerifyEmail_janala @UserID", p);
        }

        /// <summary>User_Delete_janala — soft disable a user account.</summary>
        public async Task User_DisableAccountAsync(int userId)
        {
            var p = new SqlParameter("@UserID", userId);
            await ExecAsync("EXEC User_Delete_janala @UserID", p);
        }

        // ──────────────────────────────────────────────
        // APPLICANT MODULE
        // ──────────────────────────────────────────────

        /// <summary>Applicant_UpdateResume_janala — update CV path only.</summary>
        public async Task Applicant_UpdateResumePathAsync(int applicantId, string resumePath)
        {
            await ExecAsync(
                "EXEC Applicant_UpdateResume_janala @ApplicantID, @ResumeFilePath",
                new SqlParameter("@ApplicantID",    applicantId),
                new SqlParameter("@ResumeFilePath", resumePath));
        }

        /// <summary>Applicant_UpdateSettings_janala — save privacy preferences.</summary>
        public async Task Applicant_UpdateSettingsAsync(
            int applicantId, bool profileVisibility, bool dataSharingConsent,
            string emailFrequency, bool inAppNotifications, string searchStatus)
        {
            await ExecAsync(
                "EXEC Applicant_UpdateSettings_janala @ApplicantID, @ProfileVisibility, " +
                "@DataSharingConsent, @EmailFrequency, @InAppNotifications, @SearchStatus",
                new SqlParameter("@ApplicantID",        applicantId),
                new SqlParameter("@ProfileVisibility",  profileVisibility),
                new SqlParameter("@DataSharingConsent", dataSharingConsent),
                new SqlParameter("@EmailFrequency",     emailFrequency),
                new SqlParameter("@InAppNotifications", inAppNotifications),
                new SqlParameter("@SearchStatus",       searchStatus));
        }

        // ──────────────────────────────────────────────
        // APPLICATION MODULE
        // ──────────────────────────────────────────────

        /// <summary>Application_UpdateScores_janala — persist AI match scores.</summary>
        public async Task Application_UpdateScoresAsync(
            int applicationId, decimal matchScore, decimal skillScore,
            decimal experienceScore, decimal educationScore)
        {
            await ExecAsync(
                "EXEC Application_UpdateScores_janala @ApplicationID, @MatchScore, " +
                "@SkillScore, @ExperienceScore, @EducationScore",
                new SqlParameter("@ApplicationID",   applicationId),
                new SqlParameter("@MatchScore",      matchScore),
                new SqlParameter("@SkillScore",      skillScore),
                new SqlParameter("@ExperienceScore", experienceScore),
                new SqlParameter("@EducationScore",  educationScore));
        }

        /// <summary>Application_UpdateStatus_janala — change application status + optional note.</summary>
        public async Task Application_UpdateStatusAsync(
            int applicationId, string status, string? notes = null)
        {
            await ExecAsync(
                "EXEC Application_UpdateStatus_janala @ApplicationID, @ApplicationStatus, @Notes",
                new SqlParameter("@ApplicationID",     applicationId),
                new SqlParameter("@ApplicationStatus", status),
                new SqlParameter("@Notes",             (object?)notes ?? DBNull.Value));
        }

        // ──────────────────────────────────────────────
        // INTERVIEW MODULE
        // ──────────────────────────────────────────────

        /// <summary>Interview_Save_janala — schedule a new interview round.</summary>
        public async Task Interview_ScheduleAsync(
            int applicationId, int round, DateTime date, TimeSpan time,
            string type = "Online", string? meetingLink = null, string? location = null)
        {
            await ExecAsync(
                "EXEC Interview_Save_janala @ApplicationID, @InterviewRound, @InterviewDate, " +
                "@InterviewTime, @InterviewType, @MeetingLink, @InterviewLocation",
                new SqlParameter("@ApplicationID",     applicationId),
                new SqlParameter("@InterviewRound",    round),
                new SqlParameter("@InterviewDate",     date.Date),
                new SqlParameter("@InterviewTime",     time),
                new SqlParameter("@InterviewType",     type),
                new SqlParameter("@MeetingLink",       (object?)meetingLink ?? DBNull.Value),
                new SqlParameter("@InterviewLocation", (object?)location    ?? DBNull.Value));
        }

        /// <summary>Interview_Update_janala — update result and feedback.</summary>
        public async Task Interview_UpdateResultAsync(
            int interviewId, string status, string? result = null,
            string? notes = null, string? feedback = null)
        {
            await ExecAsync(
                "EXEC Interview_Update_janala @InterviewID, @InterviewStatus, " +
                "@InterviewResult, @Notes, @Feedback",
                new SqlParameter("@InterviewID",     interviewId),
                new SqlParameter("@InterviewStatus", status),
                new SqlParameter("@InterviewResult", (object?)result   ?? DBNull.Value),
                new SqlParameter("@Notes",           (object?)notes    ?? DBNull.Value),
                new SqlParameter("@Feedback",        (object?)feedback ?? DBNull.Value));
        }

        /// <summary>Interview_Delete_janala — cancel an interview.</summary>
        public async Task Interview_CancelAsync(int interviewId)
        {
            var p = new SqlParameter("@InterviewID", interviewId);
            await ExecAsync("EXEC Interview_Delete_janala @InterviewID", p);
        }

        // ──────────────────────────────────────────────
        // SKILL MODULE
        // ──────────────────────────────────────────────

        /// <summary>ApplicantSkill_Save_janala — link skill to applicant.</summary>
        public async Task ApplicantSkill_AddAsync(int applicantId, int skillId)
        {
            await ExecAsync(
                "EXEC ApplicantSkill_Save_janala @ApplicantID, @SkillID",
                new SqlParameter("@ApplicantID", applicantId),
                new SqlParameter("@SkillID",     skillId));
        }

        /// <summary>ApplicantSkill_Delete_janala — unlink skill from applicant.</summary>
        public async Task ApplicantSkill_RemoveAsync(int applicantId, int skillId)
        {
            await ExecAsync(
                "EXEC ApplicantSkill_Delete_janala @ApplicantID, @SkillID",
                new SqlParameter("@ApplicantID", applicantId),
                new SqlParameter("@SkillID",     skillId));
        }

        // ──────────────────────────────────────────────
        // RECRUITER MODULE
        // ──────────────────────────────────────────────

        /// <summary>Recruiter_Delete_janala — deactivate recruiter.</summary>
        public async Task Recruiter_DeactivateAsync(int recruiterId)
        {
            var p = new SqlParameter("@RecruiterID", recruiterId);
            await ExecAsync("EXEC Recruiter_Delete_janala @RecruiterID", p);
        }

        // ──────────────────────────────────────────────
        // JOB MODULE
        // ──────────────────────────────────────────────

        /// <summary>Job_Delete_janala — close (soft-delete) a job posting.</summary>
        public async Task Job_CloseAsync(int jobId)
        {
            var p = new SqlParameter("@JobID", jobId);
            await ExecAsync("EXEC Job_Delete_janala @JobID", p);
        }

        // ──────────────────────────────────────────────
        // ADMIN MODULE
        // ──────────────────────────────────────────────

        /// <summary>Admin_ToggleUserStatus_janala — enable or disable any user.</summary>
        public async Task Admin_ToggleUserStatusAsync(int userId, string newStatus)
        {
            await ExecAsync(
                "EXEC Admin_ToggleUserStatus_janala @UserID, @NewStatus",
                new SqlParameter("@UserID",    userId),
                new SqlParameter("@NewStatus", newStatus));
        }
    }
}

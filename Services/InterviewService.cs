using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;

namespace AIResumeScreeningSystem.Services
{
    public class InterviewService : IInterviewService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _email;

        public InterviewService(ApplicationDbContext db, IEmailService email)
        { _db = db; _email = email; }

        public async Task<(bool success, string message)> ScheduleAsync(InterviewScheduleDTO dto)
        {
            var app = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == dto.ApplicationID);

            if (app == null) return (false, "Application not found.");

            if (app.Applicant?.User == null || app.Job == null) return (false, "Application data is incomplete.");

            if (!TimeSpan.TryParse(dto.InterviewTime, out var timespan))
                return (false, "Invalid time format.");

            var interview = new Interview
            {
                ApplicationID = dto.ApplicationID,
                InterviewRound = dto.InterviewRound,
                InterviewDate = dto.InterviewDate,
                InterviewTime = timespan,
                InterviewType = dto.InterviewType,
                MeetingLink = dto.MeetingLink,
                InterviewLocation = dto.InterviewLocation,
                InterviewStatus = "Scheduled",
                InterviewResult = "Pending",
                Notes = dto.Notes
            };

            _db.Interviews.Add(interview);
            app.ApplicationStatus = "InterviewScheduled";
            app.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            await _email.SendInterviewInvitationAsync(
                app.Applicant!.User!.Email, app.Applicant.User.FullName,
                app.Job!.JobTitle, dto.InterviewDate, timespan,
                dto.InterviewType, dto.MeetingLink);



            return (true, "Interview scheduled and invitation sent.");
        }

        public async Task<List<Interview>> GetUpcomingAsync(int recruiterId)
            => await _db.Interviews
                .Include(i => i.Application).ThenInclude(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(i => i.Application).ThenInclude(a => a.Job)
                .Where(i => i.Application.Job.RecruiterID == recruiterId
                    && i.InterviewDate >= DateTime.Today
                    && i.InterviewStatus == "Scheduled")
                .OrderBy(i => i.InterviewDate)
                .ToListAsync();

        public async Task<(bool success, string message)> CancelAsync(int interviewId)
        {
            var interview = await _db.Interviews.FindAsync(interviewId);
            if (interview == null) return (false, "Interview not found.");
            interview.InterviewStatus = "Cancelled";
            await _db.SaveChangesAsync();
            return (true, "Interview cancelled.");
        }

        public async Task<(bool success, string message)> CompleteAsync(int interviewId, string result, string? feedback)
        {
            var interview = await _db.Interviews
                .Include(i => i.Application)
                .FirstOrDefaultAsync(i => i.InterviewID == interviewId);

            if (interview == null) return (false, "Interview not found.");

            var allowedResults = new[] { "Passed", "Failed", "Pending" };
            if (!allowedResults.Contains(result)) return (false, "Invalid result. Must be 'Passed', 'Failed', or 'Pending'.");

            interview.InterviewStatus = "Completed";
            interview.InterviewResult = result;
            interview.Feedback = feedback;

            interview.Application.ApplicationStatus = "InterviewCompleted";
            interview.Application.UpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();
            return (true, $"Interview marked as completed — {result}.");
        }
    }
}

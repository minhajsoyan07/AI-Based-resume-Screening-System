using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;

namespace AIResumeScreeningSystem.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAIMatchingService _ai;
        private readonly IEmailService _email;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAutomationService _automation;

        public ApplicationService(ApplicationDbContext db, IAIMatchingService ai,
            IEmailService email, IConfiguration config,
            IServiceScopeFactory scopeFactory, IAutomationService automation)
        {
            _db = db; _ai = ai; _email = email; _config = config;
            _scopeFactory = scopeFactory; _automation = automation;
        }

        public async Task<(bool success, string message, string? trackingId)> ApplyAsync(ApplicationCreateDTO dto, int? applicantId)
        {
            string? resumePath = null;
            if (!applicantId.HasValue) 
                return (false, "Only registered applicants can apply.", null);

            var applicant = await _db.Applicants
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.ApplicantID == applicantId.Value);

            if (applicant == null) return (false, "Applicant not found.", null);
            if (applicant.User == null) return (false, "User data not found.", null);

            var email = applicant.User.Email;
            var name = applicant.User.FullName;

            // Prevent duplicate
            if (await _db.Applications.AnyAsync(a => a.ApplicantID == applicantId && a.JobID == dto.JobID))
                return (false, "You have already applied for this job.", null);

            // Handle resume selection
            if (dto.UseExistingResume)
            {
                if (string.IsNullOrEmpty(applicant.ResumeFilePath))
                    return (false, "You do not have a CV on file. Please upload a new one.", null);
                
                resumePath = applicant.ResumeFilePath;
            }
            else if (dto.ResumeFile != null && dto.ResumeFile.Length > 0)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(),
                    _config["FileStorage:ResumePath"] ?? "wwwroot/uploads/resumes/");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var fileName = $"User_{Guid.NewGuid()}{Path.GetExtension(dto.ResumeFile.FileName)}";
                var fullPath = Path.Combine(folder, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await dto.ResumeFile.CopyToAsync(stream);

                resumePath = $"/uploads/resumes/{fileName}";
                
                applicant.ResumeFilePath = resumePath;
                await _db.SaveChangesAsync();
            }

            if (string.IsNullOrEmpty(resumePath))
                return (false, "Please upload your resume before applying.", null);

            var trackingId = $"APP-{DateTime.Now.Year}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";


            var application = new Application
            {
                ApplicantID = applicantId.Value,
                JobID = dto.JobID,
                ResumeFilePath = resumePath,
                ApplicationStatus = "Applied",
                ApplicationTrackingID = trackingId,
                Notes = dto.CoverLetter
            };

            _db.Applications.Add(application);
            await _db.SaveChangesAsync();

            // Update applicant activity tracking
            applicant.LastActivityDate = DateTime.Now;
            _db.Applicants.Update(applicant);
            await _db.SaveChangesAsync();

            // AI Pipeline (async)
            _ = Task.Run(async () => {
                try {
                    using var scope = _scopeFactory.CreateScope();
                    var autoSvc = scope.ServiceProvider.GetRequiredService<IAutomationService>();
                    await autoSvc.ProcessApplicationAsync(application.ApplicationID);
                } catch { }
            });

            // Send Notifications
            var job = await _db.Jobs.FindAsync(dto.JobID);
            if (job != null && !string.IsNullOrEmpty(email))
            {
                await _email.SendApplicationConfirmationAsync(email, name ?? "Applicant", job.JobTitle);
            }

            return (true, "Application submitted successfully!", trackingId);
        }

        public async Task<List<Application>> GetByApplicantAsync(int? applicantId)
        {
            if (applicantId == null) return new List<Application>();
            return await _db.Applications
                .TagWith("ApplicationService.GetByApplicantAsync")
                .Include(a => a.Job).ThenInclude(j => j!.Recruiter).ThenInclude(r => r!.User)
                .Include(a => a.Job).ThenInclude(j => j!.Company)
                .Where(a => a.ApplicantID == applicantId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();
        }

        public async Task<List<CandidateViewModel>> GetCandidatesByJobAsync(int jobId)
        {
            var apps = await _db.Applications
                .TagWith("ApplicationService.GetCandidatesByJobAsync")
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Applicant).ThenInclude(ap => ap!.ApplicantSkills).ThenInclude(sk => sk.Skill)
                .Include(a => a.Job)
                .Where(a => a.JobID == jobId)
                .OrderByDescending(a => a.MatchScore)
                .ToListAsync();

                var results = apps.Select(a => {
                return new CandidateViewModel
                {
                    ApplicationID = a.ApplicationID,
                    ApplicantID = a.ApplicantID,
                    JobID = a.JobID,
                    FullName = a.Applicant?.User?.FullName ?? "Unknown",
                    Email = a.Applicant?.User?.Email ?? "N/A",
                    MatchScore = a.MatchScore,
                    SkillScore = a.SkillScore,
                    ExperienceScore = a.ExperienceScore,
                    EducationScore = a.EducationScore,
                    KeywordScore = 0,
                    CertificationScore = 0,
                    RankedPosition = 0,
                    ApplicationStatus = a.ApplicationStatus,
                    ResumeFilePath = a.ResumeFilePath,
                    Skills = a.Applicant != null ? string.Join(", ", a.Applicant.ApplicantSkills.Select(s => s.Skill.SkillName)) : "N/A",
                    YearsOfExperience = a.Applicant?.YearsOfExperience ?? 0,
                    AppliedDate = a.AppliedDate,
                    JobTitle = a.Job?.JobTitle ?? "Unknown",
                    OfferStatus = null,
                    MatchVerdict = a.MatchVerdict,
                    MissingRequirements = a.MissingRequirements,
                    SearchStatus = a.Applicant?.SearchStatus ?? "active",
                    ProfileVisibility = a.Applicant?.ProfileVisibility ?? true
                };
            }).ToList();

            // Sort by match score (merit-based)
            return results
                .OrderByDescending(c => c.MatchScore)
                .ToList();
        }

        public async Task<(bool success, string message)> ShortlistAsync(int applicationId)
        {
            var app = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (app == null) return (false, "Application not found.");
            if (app.Applicant?.User == null || app.Job == null) return (false, "Application data is incomplete.");

            app.ApplicationStatus = "Shortlisted";
            app.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            await _email.SendShortlistNotificationAsync(
                app.Applicant!.User!.Email, app.Applicant.User.FullName, app.Job.JobTitle);

            return (true, "Candidate shortlisted.");
        }

        public async Task<(bool success, string message)> RejectAsync(int applicationId)
        {
            var app = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (app == null) return (false, "Application not found.");
            if (app.Applicant?.User == null || app.Job == null) return (false, "Application data is incomplete.");

            app.ApplicationStatus = "Rejected";
            app.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            await _email.SendRejectionEmailAsync(
                app.Applicant!.User!.Email, app.Applicant.User.FullName, app.Job.JobTitle);

            return (true, "Candidate rejected.");
        }

        public async Task<(bool success, string message)> WithdrawAsync(int applicationId, int? applicantId)
        {
            if (applicantId == null) return (false, "Unauthorized request.");
            var app = await _db.Applications
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId && a.ApplicantID == applicantId);

            if (app == null) return (false, "Application not found.");
            if (app.ApplicationStatus != "Applied") return (false, "Cannot withdraw after review has started.");

            app.ApplicationStatus = "Withdrawn";
            app.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();
            return (true, "Application withdrawn.");
        }

        public async Task<(bool success, string message)> UpdateStatusAsync(int applicationId, string status)
        {
            var app = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (app == null) return (false, "Application not found.");

            var allowedStatuses = new[] { "Applied", "UnderReview", "Shortlisted", "Rejected", "InterviewScheduled", "InterviewCompleted", "OfferSent", "Hired", "Withdrawn" };
            if (!allowedStatuses.Contains(status)) return (false, "Invalid status value.");

            app.ApplicationStatus = status;
            app.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            if (status == "Hired")
            {
                if (app.Applicant?.User != null && app.Job != null)
                {
                    await _email.SendHiredConfirmationAsync(
                        app.Applicant!.User!.Email, app.Applicant.User.FullName, app.Job.JobTitle);
                }
            }

            return (true, $"Application status updated to {status}.");
        }

        public async Task<(bool success, string message)> BulkShortlistAsync(List<int> applicationIds)
        {
            if (applicationIds == null || !applicationIds.Any()) return (false, "No candidates selected.");

            var apps = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .Where(a => applicationIds.Contains(a.ApplicationID))
                .ToListAsync();

            foreach (var app in apps)
            {
                if (app.ApplicationStatus != "Applied" && app.ApplicationStatus != "UnderReview") continue;

                app.ApplicationStatus = "Shortlisted";
                app.UpdatedDate = DateTime.Now;

                // Send email and notification in background with proper scope
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        if (app.Applicant?.User != null && app.Job != null)
                        {
                            await emailSvc.SendShortlistNotificationAsync(app.Applicant!.User!.Email, app.Applicant.User.FullName, app.Job.JobTitle);
                        }
                    }
                    catch (Exception)
                    {
                        // Log but don't fail the bulk operation
                    }
                });
            }

            await _db.SaveChangesAsync();
            return (true, $"{apps.Count} candidate(s) bulk shortlisted successfully.");
        }

        public async Task<(bool success, string message)> BulkRejectAsync(List<int> applicationIds)
        {
            if (applicationIds == null || !applicationIds.Any()) return (false, "No candidates selected.");

            var apps = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .Where(a => applicationIds.Contains(a.ApplicationID))
                .ToListAsync();

            foreach (var app in apps)
            {
                // Only allow rejection from early phases
                if (app.ApplicationStatus != "Applied" && app.ApplicationStatus != "UnderReview" && app.ApplicationStatus != "InterviewCompleted") continue;

                app.ApplicationStatus = "Rejected";
                app.UpdatedDate = DateTime.Now;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        if (app.Applicant?.User != null && app.Job != null)
                        {
                            await emailSvc.SendRejectionEmailAsync(app.Applicant!.User!.Email, app.Applicant.User.FullName, app.Job.JobTitle);
                        }
                    }
                    catch (Exception)
                    {
                        // Log but don't fail the bulk operation
                    }
                });
            }

            await _db.SaveChangesAsync();
            return (true, $"{apps.Count} candidate(s) bulk rejected.");
        }

        public async Task<(bool success, string message)> AddNoteAsync(int applicationId, string notes)
        {
            var app = await _db.Applications.FindAsync(applicationId);
            if (app == null) return (false, "Application not found.");

            app.Notes = notes;
            app.UpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();
            return (true, "Candidate notes updated successfully.");
        }

        public async Task<(bool success, string message)> RunAnalysisAsync(int jobId)
        {
            var apps = await _db.Applications
                .Where(a => a.JobID == jobId)
                .ToListAsync();

            if (!apps.Any()) return (false, "No applications found for this job.");

            // Clear cache to ensure fresh calculations
            AIResumeScreeningSystem.Services.AIMatchingService.ClearCache();

            foreach (var app in apps)
            {
                await _ai.CalculateMatchScoreAsync(app.ApplicationID);
            }

            return (true, $"AI Analysis completed for {apps.Count} candidates.");
        }
    }
}

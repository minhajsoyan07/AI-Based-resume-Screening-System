using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AIResumeScreeningSystem.Controllers
{
    [Route("api/applications")]
    [Authorize]
    public class ApplicationsApiController : BaseApiController
    {
        private readonly ApplicationDbContext _db;
        private readonly IApplicationService _appService;
        private readonly IAIMatchingService _matchingService;
        private readonly ILogger<ApplicationsApiController> _logger;

        public ApplicationsApiController(
            ApplicationDbContext db,
            IApplicationService appService,
            IAIMatchingService matchingService,
            ILogger<ApplicationsApiController> logger)
        {
            _db = db;
            _appService = appService;
            _matchingService = matchingService;
            _logger = logger;
        }

        [HttpPost("apply")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Apply([FromForm] ApplicationCreateDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid input data" });

            var userId = CurrentUserID;
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == userId);
            var applicantId = applicant?.ApplicantID;

            if (applicantId == null)
                return BadRequest(new { success = false, message = "Applicant profile not found" });

            var (success, message, trackingId) = await _appService.ApplyAsync(model, applicantId);

            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new
            {
                success = true,
                message,
                data = new
                {
                    trackingId
                }
            });
        }


        [HttpGet]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> GetMyApplications([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == userId);

            if (applicant == null)
                return BadRequest(new { success = false, message = "Applicant profile not found" });

            var query = _db.Applications
                .Include(a => a.Job).ThenInclude(j => j.Company)
                .Where(a => a.ApplicantID == applicant.ApplicantID);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.ApplicationStatus == status);

            var totalCount = await query.CountAsync();
            var applications = await query
                .OrderByDescending(a => a.AppliedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    applications = applications.Select(a => new
                    {
                        applicationId = a.ApplicationID,
                        jobId = a.JobID,
                        jobTitle = a.Job?.JobTitle,
                        companyName = a.Job?.Company?.CompanyName,
                        location = a.Job?.JobLocation,
                        matchScore = a.MatchScore,
                        status = a.ApplicationStatus,
                        appliedDate = a.AppliedDate,
                        updatedDate = a.UpdatedDate
                    })
                }
            });
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetApplication(int id)
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var user = await _db.Users.FindAsync(userId);
            var isAdmin = user?.Role == "Admin";

            var application = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job).ThenInclude(j => j!.Company)
                .Include(a => a.Job).ThenInclude(j => j!.Recruiter).ThenInclude(r => r!.User)
                .Include(a => a.Interviews)
                .FirstOrDefaultAsync(a => a.ApplicationID == id);

            if (application == null)
                return NotFound(new { success = false, message = "Application not found" });

            var applicantId = application.ApplicantID;
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == CurrentUserID);
            var isOwner = applicant != null && application.ApplicantID == applicant.ApplicantID;

            if (!isOwner && !isAdmin && user?.Role != "Recruiter")
                return Forbid();

            return Ok(new
            {
                success = true,
                data = new
                {
                    applicationId = application.ApplicationID,
                    jobId = application.JobID,
                    jobTitle = application.Job?.JobTitle,
                    companyName = application.Job?.Company?.CompanyName,
                    location = application.Job?.JobLocation,
                    jobDescription = application.Job?.JobDescription,
                    requiredSkills = application.Job?.RequiredSkills,
                    matchScore = application.MatchScore,
                    status = application.ApplicationStatus,
                    appliedDate = application.AppliedDate,
                    updatedDate = application.UpdatedDate,
                    applicant = new
                    {
                        name = application.Applicant?.User?.FullName,
                        email = application.Applicant?.User?.Email,
                        phone = application.Applicant?.User?.PhoneNumber,
                        yearsOfExperience = application.Applicant?.YearsOfExperience,
                        highestEducation = application.Applicant?.HighestEducation,
                        profileSummary = application.Applicant?.ProfileSummary
                    },
                    interviews = application.Interviews.Select(i => new
                    {
                        id = i.InterviewID,
                        scheduledDate = i.InterviewDate,
                        scheduledTime = i.InterviewTime.ToString(),
                        interviewType = i.InterviewType,
                        location = i.InterviewLocation,
                        meetingLink = i.MeetingLink,
                        status = i.InterviewStatus,
                        result = i.InterviewResult,
                        notes = i.Notes,
                        feedback = i.Feedback
                    })
                }
            });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Recruiter,Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto model)
        {
            var application = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == id);

            if (application == null)
                return NotFound(new { success = false, message = "Application not found" });

            var oldStatus = application.ApplicationStatus;
            application.ApplicationStatus = model.Status;
            application.UpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Status updated successfully",
                data = new
                {
                    applicationId = application.ApplicationID,
                    oldStatus,
                    newStatus = application.ApplicationStatus,
                    updatedDate = application.UpdatedDate
                }
            });
        }

        [HttpPost("{id}/shortlist")]
        [Authorize(Roles = "Recruiter,Admin")]
        public async Task<IActionResult> Shortlist(int id)
        {
            var application = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == id);

            if (application == null)
                return NotFound(new { success = false, message = "Application not found" });

            application.ApplicationStatus = "Shortlisted";
            application.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Candidate shortlisted" });
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Recruiter,Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var application = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == id);

            if (application == null)
                return NotFound(new { success = false, message = "Application not found" });

            application.ApplicationStatus = "Rejected";
            application.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Application rejected" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserID == userId);

            var application = await _db.Applications.FindAsync(id);

            if (application == null)
                return NotFound(new { success = false, message = "Application not found" });

            if (applicant == null || application.ApplicantID != applicant.ApplicantID)
                return Forbid();

            application.ApplicationStatus = "Withdrawn";
            application.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Application withdrawn successfully" });
        }
    }
}
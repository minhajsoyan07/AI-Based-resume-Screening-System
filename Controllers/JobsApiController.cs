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
    public class JobsApiController : BaseApiController
    {
        private readonly ApplicationDbContext _db;
        private readonly IJobService _jobService;
        private readonly ILogger<JobsApiController> _logger;

        public JobsApiController(ApplicationDbContext db, IJobService jobService, ILogger<JobsApiController> logger)
        {
            _db = db;
            _jobService = jobService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetJobs([FromQuery] string? search, [FromQuery] string? location, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var jobs = await _jobService.GetAllJobsAsync(search, location);
            
            var totalCount = jobs.Count;
            var pagedJobs = jobs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    jobs = pagedJobs.Select(j => new
                    {
                        jobId = j.JobID,
                        title = j.JobTitle,
                        description = j.JobDescription,
                        requiredSkills = j.RequiredSkills,
                        location = j.JobLocation,
                        jobType = j.JobType,
                        salaryMin = j.SalaryMin,
                        salaryMax = j.SalaryMax,
                        experience = j.MinimumExperience,
                        education = j.EducationLevel,
                        status = j.JobStatus,
                        companyName = j.Company?.CompanyName,
                        postedDate = j.CreatedDate,
                        deadline = j.ApplicationDeadline
                    })
                }
            });
        }

        [HttpGet("SearchSuggestions")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchSuggestions([FromQuery] string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
                return Ok(new List<string>());

            // Get job titles that match the search term
            var jobTitles = await _db.Jobs
                .Where(j => j.JobStatus == "Open" && 
                         (j.JobTitle.ToLower().Contains(term.ToLower()) ||
                          (j.RequiredSkills != null && j.RequiredSkills.ToLower().Contains(term.ToLower()))))
                .Select(j => j.JobTitle)
                .Distinct()
                .Take(10)
                .ToListAsync();

            // Get company names that match the search term
            var companyNames = await _db.Jobs
                .Where(j => j.JobStatus == "Open" && 
                         (j.Company != null && j.Company.CompanyName.ToLower().Contains(term.ToLower())))
                .Select(j => j.Company!.CompanyName)
                .Distinct()
                .Take(5)
                .ToListAsync();

            // Get skills that match the search term
            var skills = await _db.JobSkills
                .Where(js => js.Skill != null && js.Skill.SkillName.ToLower().Contains(term.ToLower()))
                .Select(js => js.Skill!.SkillName)
                .Distinct()
                .Take(5)
                .ToListAsync();

            // Combine and deduplicate suggestions
            var suggestions = new List<string>();
            suggestions.AddRange(jobTitles);
            suggestions.AddRange(companyNames);
            suggestions.AddRange(skills);
            
            // Remove duplicates while preserving order
            var uniqueSuggestions = new List<string>();
            foreach (var suggestion in suggestions)
            {
                if (!uniqueSuggestions.Contains(suggestion, StringComparer.OrdinalIgnoreCase))
                {
                    uniqueSuggestions.Add(suggestion);
                }
            }

            return Ok(uniqueSuggestions.Take(8)); // Return top 8 suggestions
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetJob(int id)
        {
            var job = await _jobService.GetJobByIdAsync(id);
            if (job == null)
                return NotFound(new { success = false, message = "Job not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    jobId = job.JobID,
                    title = job.JobTitle,
                    description = job.JobDescription,
                    requiredSkills = job.RequiredSkills,
                    location = job.JobLocation,
                    jobType = job.JobType,
                    salaryMin = job.SalaryMin,
                    salaryMax = job.SalaryMax,
                    experience = job.MinimumExperience,
                    education = job.EducationLevel,
                    status = job.JobStatus,
                    companyName = job.Company?.CompanyName,
                    companyWebsite = job.Company?.CompanyWebsite,
                    postedDate = job.CreatedDate,
                    deadline = job.ApplicationDeadline,
                    recruiterName = job.Recruiter?.User?.FullName
                }
            });
        }

        [HttpPost]
        [Authorize(Roles = "Recruiter,Admin")]
        public async Task<IActionResult> CreateJob([FromBody] JobCreateDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid input data" });

            // Validate ApplicationDeadline
            if (!model.ApplicationDeadline.HasValue)
                return BadRequest(new { success = false, message = "Application deadline is required" });

            if (model.ApplicationDeadline.Value.Date < DateTime.Today.AddDays(1))
                return BadRequest(new { success = false, message = "Application deadline must be at least 1 day in the future" });

            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var recruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserID == userId);
            
            if (recruiter == null)
                return BadRequest(new { success = false, message = "Recruiter profile not found" });

            var job = new Job
            {
                RecruiterID = recruiter.RecruiterID,
                CompanyID = recruiter.CompanyID,
                JobTitle = model.JobTitle,
                JobDescription = model.JobDescription,
                RequiredSkills = model.RequiredSkills,
                MinimumExperience = model.MinimumExperience,
                EducationLevel = model.EducationLevel,
                JobLocation = model.JobLocation,
                SalaryMin = model.SalaryMin,
                SalaryMax = model.SalaryMax,
                JobType = model.JobType,
                JobStatus = "Open",
                ApplicationDeadline = model.ApplicationDeadline.Value
            };

            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Job created successfully", data = new { jobId = job.JobID } });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Recruiter,Admin")]
        public async Task<IActionResult> UpdateJob(int id, [FromBody] JobCreateDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid input data" });

            // Validate ApplicationDeadline if provided
            if (model.ApplicationDeadline.HasValue && model.ApplicationDeadline.Value.Date < DateTime.Today.AddDays(1))
                return BadRequest(new { success = false, message = "Application deadline must be at least 1 day in the future" });

            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var recruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserID == userId);
            
            var job = await _db.Jobs.FindAsync(id);
            if (job == null)
                return NotFound(new { success = false, message = "Job not found" });

            if (User.IsInRole("Recruiter") && job.RecruiterID != recruiter?.RecruiterID)
                return Forbid();

            job.JobTitle = model.JobTitle;
            job.JobDescription = model.JobDescription;
            job.RequiredSkills = model.RequiredSkills;
            job.MinimumExperience = model.MinimumExperience;
            job.EducationLevel = model.EducationLevel;
            job.JobLocation = model.JobLocation;
            job.SalaryMin = model.SalaryMin;
            job.SalaryMax = model.SalaryMax;
            job.JobType = model.JobType;
            job.ApplicationDeadline = model.ApplicationDeadline ?? job.ApplicationDeadline;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Job updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Recruiter,Admin")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var userId = CurrentUserID;
            if (userId == null) return Unauthorized();
            var recruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserID == userId);

            var job = await _db.Jobs.FindAsync(id);
            if (job == null)
                return NotFound(new { success = false, message = "Job not found" });

            if (User.IsInRole("Recruiter") && job.RecruiterID != recruiter?.RecruiterID)
                return Forbid();

            job.JobStatus = "Deleted";
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Job deleted successfully" });
        }

        [HttpGet("{id}/applicants")]
        [Authorize(Roles = "Recruiter,Admin")]
        public async Task<IActionResult> GetApplicants(int id, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var job = await _db.Jobs.FindAsync(id);
            if (job == null)
                return NotFound(new { success = false, message = "Job not found" });

            var query = _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Where(a => a.JobID == id);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.ApplicationStatus == status);

            var totalCount = await query.CountAsync();
            var applications = await query
                .OrderByDescending(a => a.MatchScore)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    jobId = id,
                    jobTitle = job.JobTitle,
                    totalApplicants = totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    applicants = applications.Select(a => new
                    {
                        applicationId = a.ApplicationID,
                        applicantId = a.ApplicantID,
                        name = a.Applicant?.User?.FullName,
                        email = a.Applicant?.User?.Email,
                        phone = a.Applicant?.User?.PhoneNumber,
                        matchScore = a.MatchScore,
                        status = a.ApplicationStatus,
                        appliedDate = a.AppliedDate,
                        skillScore = a.SkillScore,
                        experienceScore = a.ExperienceScore,
                        educationScore = a.EducationScore
                    })
                }
            });
        }
    }
}
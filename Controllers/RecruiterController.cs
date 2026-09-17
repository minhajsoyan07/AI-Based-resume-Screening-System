using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

using AIResumeScreeningSystem.Constants;

namespace AIResumeScreeningSystem.Controllers
{
    [Authorize(Roles = "Recruiter")]
    public class RecruiterController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IJobService _jobs;
        private readonly IApplicationService _apps;
        private readonly IInterviewService _interviews;
        private readonly IAIMatchingService _matching;
        private readonly IWebHostEnvironment _env;

        public RecruiterController(ApplicationDbContext db, IJobService jobs,
            IApplicationService apps, IInterviewService interviews,
            IAIMatchingService matching, IWebHostEnvironment env)
        {
            _db = db; _jobs = jobs; _apps = apps; _interviews = interviews;
            _matching = matching;
            _env = env;
        }


        private async Task<int?> GetRecruiterIDAsync()
        {
            var userId = CurrentUserID;
            if (userId == null) return null;
            return (await _db.Recruiters.AsNoTracking().FirstOrDefaultAsync(r => r.UserID == userId))?.RecruiterID;
        }

        // ── DASHBOARD ──
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserID!.Value;
            var recruiter = await _db.Recruiters
                .AsNoTracking()
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.UserID == userId);

            if (recruiter == null) return RedirectToAction("CompanySetup");
            var recruiterId = recruiter.RecruiterID;

            var myJobs = await _db.Jobs
                .AsNoTracking()
                .Include(j => j.Applications)
                .Where(j => j.RecruiterID == recruiterId)
                .OrderByDescending(j => j.CreatedDate)
                .ToListAsync();

            var jobIds = myJobs.Select(j => j.JobID).ToList();
            var allApps = await _db.Applications
                .AsNoTracking()
                .Where(a => jobIds.Contains(a.JobID))
                .ToListAsync();

            var upcoming = await _interviews.GetUpcomingAsync(recruiterId);

            // Compute dynamic trends
            var now = DateTime.Now;
            var last7 = now.AddDays(-7);
            var prev7 = now.AddDays(-14);

            var recentApps = allApps.Count(a => a.AppliedDate >= last7);
            var previousApps = allApps.Count(a => a.AppliedDate >= prev7 && a.AppliedDate < last7);

            double applicantTrendPercent = 0;
            string applicantTrendDirection = "neutral";
            if (previousApps > 0)
            {
                applicantTrendPercent = Math.Round(((double)(recentApps - previousApps) / previousApps) * 100, 1);
                applicantTrendDirection = applicantTrendPercent > 0 ? "up" : applicantTrendPercent < 0 ? "down" : "neutral";
            }
            else if (recentApps > 0)
            {
                applicantTrendPercent = 100;
                applicantTrendDirection = "up";
            }

            var openJobs = myJobs.Count(j => j.JobStatus == "Open");
            var activeJobsTrend = openJobs > 0 ? $"{openJobs} active" : "No active jobs";

            var avgScore = allApps.Any() ? allApps.Average(a => (double)a.MatchScore) : 0;
            var matchScoreTrend = avgScore >= 70 ? "High quality" : avgScore >= 40 ? "Moderate" : "Needs improvement";

            var next7Interviews = upcoming.Count(i => i.InterviewDate <= now.AddDays(7));
            var interviewsTrend = next7Interviews > 0 ? $"{next7Interviews} upcoming" : "None scheduled";

            var stats = new DashboardStatsViewModel
            {
                TotalJobs = myJobs.Count,
                TotalApplicants = allApps.Count,
                ShortlistedCandidates = allApps.Count(a => a.ApplicationStatus == "Shortlisted"),
                ScheduledInterviews = upcoming.Count,
                OffersSent = allApps.Count(a => a.ApplicationStatus == "OfferSent"),
                HiredCount = allApps.Count(a => a.ApplicationStatus == "Hired"),
                AverageMatchScore = avgScore,
                ApplicantTrendPercent = applicantTrendPercent,
                ApplicantTrendDirection = applicantTrendDirection,
                ActiveJobsTrend = activeJobsTrend,
                MatchScoreTrend = matchScoreTrend,
                InterviewsTrend = interviewsTrend,
                InterviewsNext7Days = next7Interviews
            };

            if (myJobs.Any())
            {
                stats.RecentCandidates = await _apps.GetCandidatesByJobAsync(myJobs.First().JobID);
            }

            ViewBag.Jobs = myJobs;
            ViewBag.Interviews = upcoming;
            return View(stats);
        }

        // ── COMPANY SETUP ──
        [HttpGet]
        public async Task<IActionResult> CompanySetup()
        {
            var recruiterId = await GetRecruiterIDAsync();
            if (recruiterId != null) return RedirectToAction("Index"); // Already setup
            return View(new CompanySetupDTO());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanySetup(CompanySetupDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var userId = CurrentUserID;
            if (userId == null) return RedirectToAction("Login", "Auth");

            string? logoPath = await UploadFileAsync(dto.LogoFile);
            string? coverPath = await UploadFileAsync(dto.CoverPhotoFile);
            string? sigPath = await UploadFileAsync(dto.SignatureFile);

            var company = new Company
            {
                CompanyName = dto.CompanyName,
                CompanyNameBangla = dto.CompanyNameBangla,
                OrganizationCategory = dto.OrganizationCategory,
                IndustryType = dto.IndustryType,
                CompanyWebsite = dto.CompanyWebsite,
                Tags = dto.Tags,
                Tagline = dto.Tagline,
                LogoPath = logoPath,
                CoverPhotoPath = coverPath,
                SignaturePath = sigPath,
                EstablishmentYear = dto.EstablishmentYear,
                CompanySize = dto.CompanySize,
                OrganizationEmail = dto.OrganizationEmail,
                AboutOrganization = dto.AboutOrganization,
                Vision = dto.Vision,
                Mission = dto.Mission,
                Division = dto.Division,
                District = dto.District,
                Thana = dto.Thana,
                PostalCode = dto.PostalCode,
                FullAddress = dto.FullAddress,
                FullAddressBangla = dto.FullAddressBangla,
                MobileNumber = dto.MobileNumber,
                LandPhoneNumber = dto.LandPhoneNumber
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var recruiter = new Recruiter
            {
                UserID = userId.Value,
                CompanyID = company.CompanyID,
                Designation = "HR Manager" // Default, can be updated later
            };

            _db.Recruiters.Add(recruiter);
            await _db.SaveChangesAsync();



            TempData["Success"] = "Company Profile setup successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── PROFILE / ORGANIZATION DETAILS ──
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var recruiterId = await GetRecruiterIDAsync();
            if (recruiterId == null) return RedirectToAction("CompanySetup");

            var recruiter = await _db.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.RecruiterID == recruiterId.Value);

            if (recruiter?.Company == null) return RedirectToAction("CompanySetup");

            var c = recruiter.Company;
            var dto = new CompanySetupDTO
            {
                CompanyName = c.CompanyName,
                CompanyNameBangla = c.CompanyNameBangla,
                OrganizationCategory = c.OrganizationCategory ?? "",
                IndustryType = c.IndustryType ?? "",
                CompanyWebsite = c.CompanyWebsite,
                Tags = c.Tags,
                Tagline = c.Tagline,
                EstablishmentYear = c.EstablishmentYear,
                CompanySize = c.CompanySize,
                OrganizationEmail = c.OrganizationEmail ?? "",
                AboutOrganization = c.AboutOrganization,
                Vision = c.Vision,
                Mission = c.Mission,
                Division = c.Division,
                District = c.District,
                Thana = c.Thana,
                PostalCode = c.PostalCode,
                FullAddress = c.FullAddress,
                FullAddressBangla = c.FullAddressBangla,
                MobileNumber = c.MobileNumber,
                LandPhoneNumber = c.LandPhoneNumber
            };

            ViewBag.LogoPath = c.LogoPath;
            ViewBag.CoverPath = c.CoverPhotoPath;
            ViewBag.SignaturePath = c.SignaturePath;

            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(CompanySetupDTO dto)
        {
            var recruiterId = await GetRecruiterIDAsync();
            if (recruiterId == null) return RedirectToAction("Login", "Auth");

            var recruiter = await _db.Recruiters
                .Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.RecruiterID == recruiterId.Value);

            if (recruiter?.Company == null) return NotFound();

            var company = recruiter.Company;

            if (!ModelState.IsValid)
            {
                ViewBag.LogoPath = company.LogoPath;
                ViewBag.CoverPath = company.CoverPhotoPath;
                ViewBag.SignaturePath = company.SignaturePath;
                return View(dto);
            }

            // Update fields
            company.CompanyName = dto.CompanyName;
            company.CompanyNameBangla = dto.CompanyNameBangla;
            company.OrganizationCategory = dto.OrganizationCategory;
            company.IndustryType = dto.IndustryType;
            company.CompanyWebsite = dto.CompanyWebsite;
            company.Tags = dto.Tags;
            company.Tagline = dto.Tagline;
            company.EstablishmentYear = dto.EstablishmentYear;
            company.CompanySize = dto.CompanySize;
            company.OrganizationEmail = dto.OrganizationEmail;
            company.AboutOrganization = dto.AboutOrganization;
            company.Vision = dto.Vision;
            company.Mission = dto.Mission;
            company.Division = dto.Division;
            company.District = dto.District;
            company.Thana = dto.Thana;
            company.PostalCode = dto.PostalCode;
            company.FullAddress = dto.FullAddress;
            company.FullAddressBangla = dto.FullAddressBangla;
            company.MobileNumber = dto.MobileNumber;
            company.LandPhoneNumber = dto.LandPhoneNumber;

            // Handle file updates if provided
            if (dto.LogoFile != null) company.LogoPath = await UploadFileAsync(dto.LogoFile);
            if (dto.CoverPhotoFile != null) company.CoverPhotoPath = await UploadFileAsync(dto.CoverPhotoFile);
            if (dto.SignatureFile != null) company.SignaturePath = await UploadFileAsync(dto.SignatureFile);

            await _db.SaveChangesAsync();


            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        private async Task<string?> UploadFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            
            return "/uploads/" + uniqueFileName;
        }

        // ── POST JOB ──
        [HttpGet]
        public async Task<IActionResult> PostJob()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PostJob(JobCreateDTO dto)
        {
            
            if (!ModelState.IsValid) return View(dto);

            var recruiterId = await GetRecruiterIDAsync();
            if (recruiterId == null) return RedirectToAction("Login", "Auth");

            var (success, message, _) = await _jobs.CreateJobAsync(dto, recruiterId.Value);
            TempData[success ? "Success" : "Error"] = message;

            if (!success)
            {
                ModelState.AddModelError("ApplicationDeadline", message);
                return View(dto);
            }

            return success ? RedirectToAction(nameof(Jobs)) : View(dto);
        }

        // ── MANAGE JOBS ──
        public async Task<IActionResult> Jobs()
        {
            
            var recruiterId = await GetRecruiterIDAsync();
            if (recruiterId == null) return RedirectToAction("Login", "Auth");
            var jobs = await _jobs.GetJobsByRecruiterAsync(recruiterId.Value);
            return View(jobs);
        }

        // ── CANDIDATES ──
        public async Task<IActionResult> Candidates(int jobId = 0, string? status = null, string? sort = null, string? search = null)
        {
            var recruiterId = await GetRecruiterIDAsync();
            if (recruiterId == null) return RedirectToAction("Login", "Auth");

            List<CandidateViewModel> candidates;
            Job? job = null;
            if (jobId == 0)
            {
                // Global Discovery (Find Talent) - ONLY show profiles where Visibility is true
                var pool = await _db.Applicants
                    .Include(a => a.User)
                    .Include(a => a.ApplicantSkills).ThenInclude(s => s.Skill)
                    .Where(a => a.ProfileVisibility == true)
                    .ToListAsync();

                candidates = pool.Select(a => new CandidateViewModel
                {
                    ApplicantID = a.ApplicantID,
                    FullName = a.User?.FullName ?? "Unknown",
                    Email = a.User?.Email ?? "N/A",
                    Skills = string.Join(", ", a.ApplicantSkills.Select(s => s.Skill.SkillName)),
                    YearsOfExperience = a.YearsOfExperience,
                    ApplicationStatus = "Available",
                    SearchStatus = a.SearchStatus ?? "active",
                    ProfileVisibility = a.ProfileVisibility,
                    ResumeFilePath = a.ResumeFilePath
                }).ToList();
            }
            else
            {
                job = await _jobs.GetJobByIdAsync(jobId);
                if (job == null || job.RecruiterID != recruiterId)
                {
                    TempData["Error"] = "Job not found or you do not have access to this job.";
                    return RedirectToAction(nameof(Jobs));
                }
                candidates = await _apps.GetCandidatesByJobAsync(jobId);
                ViewBag.JobTitle = job.JobTitle;
            }

            // 1. Filter by Status
            if (!string.IsNullOrEmpty(status))
                candidates = candidates.Where(c => c.ApplicationStatus == status).ToList();

            // 2. Filter by Search Query
            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                candidates = candidates.Where(c => 
                    c.FullName.ToLower().Contains(lowerSearch) || 
                    c.Email.ToLower().Contains(lowerSearch) ||
                    (!string.IsNullOrEmpty(c.Skills) && c.Skills.ToLower().Contains(lowerSearch))
                ).ToList();
            }

            // 3. Sort Candidates natively
            candidates = sort switch
            {
                "ScoreDesc" => candidates.OrderByDescending(c => c.MatchScore).ToList(),
                "SkillDesc" => candidates.OrderByDescending(c => c.SkillScore).ToList(),
                "ExpDesc"   => candidates.OrderByDescending(c => c.ExperienceScore).ToList(),
                "KwDesc"    => candidates.OrderByDescending(c => c.KeywordScore).ToList(),
                "Newest"    => candidates.OrderByDescending(c => c.AppliedDate).ToList(),
                _           => candidates.OrderByDescending(c => c.MatchScore).ToList() // Default
            };

            // Re-assign Rank just for the UI display based on the current sort
            for (int i = 0; i < candidates.Count; i++)
            {
                candidates[i].RankedPosition = i + 1;
            }

            ViewBag.JobId = jobId;
            ViewBag.StatusFilter = status;
            ViewBag.SortFilter = sort;
            ViewBag.SearchFilter = search;
            ViewBag.JobTitle = job?.JobTitle;

            return View(candidates);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkShortlist(List<int> applicationIds, int jobId)
        {
            var (success, message) = await _apps.BulkShortlistAsync(applicationIds);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Candidates), new { jobId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkReject(List<int> applicationIds, int jobId)
        {
            var (success, message) = await _apps.BulkRejectAsync(applicationIds);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Candidates), new { jobId });
        }

        [HttpGet]
        public async Task<IActionResult> CandidateAnalysis(int applicationId)
        {
            var app = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (app == null) return NotFound("Application not found.");

            // Verify access
            var recruiterId = await GetRecruiterIDAsync();
            if (app.Job!.RecruiterID != recruiterId) return Unauthorized();

            ViewBag.ApplicationId = applicationId;
            return View(app);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateDeepAnalysisApi(int applicationId)
        {
            // Verify access
            var recruiterId = await GetRecruiterIDAsync();
            var app = await _db.Applications.Include(a => a.Job).FirstOrDefaultAsync(a => a.ApplicationID == applicationId);
            if (app == null || app.Job!.RecruiterID != recruiterId) return Unauthorized();

            var markdownResult = await _matching.GenerateDeepAnalysisAsync(applicationId);
            return Json(new { success = true, data = markdownResult });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RunAnalysis(int jobId)
        {
            var (success, message) = await _apps.RunAnalysisAsync(jobId);
            TempData[success ? "Success" : "Error"] = message;
            
            return RedirectToAction(nameof(Candidates), new { jobId });
        }

        [HttpGet]
        public async Task<IActionResult> GetMatchDetails(int applicationId)
        {
            var app = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (app == null) return NotFound();

            var missingList = string.IsNullOrEmpty(app.MissingRequirements) 
                ? new List<string>() 
                : app.MissingRequirements.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

            return Json(new
            {
                success = true,
                data = new
                {
                    matchScore = app.MatchScore,
                    skillScore = app.SkillScore,
                    experienceScore = app.ExperienceScore,
                    educationScore = app.EducationScore,
                    keywordScore = 0,
                    certificationScore = 0,
                    verdict = app.MatchVerdict ?? "Analysis Pending",
                    missingRequirements = missingList,
                    notes = app.Notes
                }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int applicationId, int jobId, string notes)
        {
            var (success, message) = await _apps.AddNoteAsync(applicationId, notes);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Candidates), new { jobId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Shortlist(int applicationId, int jobId)
        {
            var (success, message) = await _apps.ShortlistAsync(applicationId);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Candidates), new { jobId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int applicationId, int jobId)
        {
            var (success, message) = await _apps.RejectAsync(applicationId);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Candidates), new { jobId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int applicationId, string status, int jobId)
        {
            var (success, message) = await _apps.UpdateStatusAsync(applicationId, status);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Candidates), new { jobId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatusAjax([FromBody] UpdateStatusDto request)
        {
            var (success, message) = await _apps.UpdateStatusAsync(request.ApplicationId, request.Status);
            return Json(new { success, message });
        }



        // ── INTERVIEWS ──
        [HttpGet]
        public async Task<IActionResult> ScheduleInterview(int applicationId)
        {
            
            var app = await _db.Applications
                .Include(a => a.Applicant).ThenInclude(ap => ap!.User)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);
            if (app != null)
                ViewBag.Application = app;
            return View(new InterviewScheduleDTO { ApplicationID = applicationId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleInterview(InterviewScheduleDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var (success, message) = await _interviews.ScheduleAsync(dto);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Interviews));
        }

        public async Task<IActionResult> Interviews()
        {
            
            var recruiterId = await GetRecruiterIDAsync();
            if (recruiterId == null) return RedirectToAction("Login", "Auth");
            var upcoming = await _interviews.GetUpcomingAsync(recruiterId.Value);
            return View(upcoming);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteInterview(int interviewId, string result, string? feedback)
        {
            var (success, message) = await _interviews.CompleteAsync(interviewId, result, feedback);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Interviews));
        }



        // ── EDIT JOB ──
        [HttpGet]
        public async Task<IActionResult> EditJob(int jobId)
        {
            var recruiterId = await GetRecruiterIDAsync();
            var job = await _jobs.GetJobByIdAsync(jobId);
            if (job == null) return NotFound();
            if (job.RecruiterID != recruiterId) return Forbid();

            var dto = new JobCreateDTO
            {
                JobTitle = job.JobTitle,
                JobDescription = job.JobDescription,
                RequiredSkills = job.RequiredSkills,
                MinimumExperience = job.MinimumExperience,
                EducationLevel = job.EducationLevel,
                JobLocation = job.JobLocation,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                JobType = job.JobType,
                JobSector = job.JobSector,
                ApplicationDeadline = job.ApplicationDeadline,
                AttachmentPath = job.AttachmentPath
            };

            ViewBag.JobId = jobId;
            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(int jobId, JobCreateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.JobId = jobId;
                return View(dto);
            }

            var (success, message) = await _jobs.UpdateJobAsync(jobId, dto);
            TempData[success ? "Success" : "Error"] = message;

            if (!success)
            {
                ViewBag.JobId = jobId;
                ModelState.AddModelError("ApplicationDeadline", message);
                return View(dto);
            }

            return RedirectToAction(nameof(Jobs));
        }

        // ── CLOSE JOB ──
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseJob(int jobId)
        {
            var recruiterId = await GetRecruiterIDAsync();
            var job = await _jobs.GetJobByIdAsync(jobId);
            if (job == null) return NotFound();
            if (job.RecruiterID != recruiterId) return Forbid();

            await _jobs.CloseJobAsync(jobId);
            TempData["Success"] = "Job closed.";
            return RedirectToAction(nameof(Jobs));
        }
    }
}

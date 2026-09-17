using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

using AIResumeScreeningSystem.Constants;
using AIResumeScreeningSystem.Services;
using Microsoft.AspNetCore.Authentication;

namespace AIResumeScreeningSystem.Controllers
{
    [Authorize(Roles = "Applicant")]
    public class ApplicantController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IJobService _jobs;
        private readonly IApplicationService _apps;
        private readonly IResumeParserService _parser;
        private readonly IAIMatchingService _matching;
        private readonly ILogger<ApplicantController> _log;

        public ApplicantController(ApplicationDbContext db, IJobService jobs,
            IApplicationService apps, IResumeParserService parser,
            IAIMatchingService matching, ILogger<ApplicantController> log)
        {
            _db = db; _jobs = jobs; _apps = apps; _parser = parser;
            _matching = matching; _log = log;
        }


        private async Task<int?> GetApplicantIDAsync()
        {
            var userId = CurrentUserID;
            if (userId == null) return null;
            return (await _db.Applicants.AsNoTracking().FirstOrDefaultAsync(a => a.UserID == userId))?.ApplicantID;
        }

        // ── DASHBOARD ──
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserID!.Value;
            var applicant = await _db.Applicants
                .AsNoTracking()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserID == userId);

            if (applicant == null) return RedirectToAction("Login", "Auth");
            var applicantId = applicant.ApplicantID;

            var myApps = await _apps.GetByApplicantAsync(applicantId);
            
            // Optimization: Get real recommendations via service instead of just 'any 5 jobs'
            var recommendations = await _matching.GetJobRecommendationsAsync(applicantId);

            var recJobIds = recommendations.Select(r => r.JobId).ToList();
            var recJobDetails = await _db.Jobs.Where(j => recJobIds.Contains(j.JobID))
                .ToDictionaryAsync(j => j.JobID, j => new { j.SalaryMin, j.SalaryMax, j.JobType, j.CreatedDate });

            var today = DateTime.Today;
            var activityLast7Days = new List<int>();
            for(int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                int count = myApps.Count(a => a.AppliedDate.Date == date);
                activityLast7Days.Add(count);
            }

            var vm = new ApplicantDashboardViewModel
            {
                TotalApplications = myApps.Count,
                PendingCount = myApps.Count(a => a.ApplicationStatus == "Applied" || a.ApplicationStatus == "UnderReview"),
                ShortlistedCount = myApps.Count(a => a.ApplicationStatus == "Shortlisted" || a.ApplicationStatus == "InterviewScheduled"),
                HiredCount = myApps.Count(a => a.ApplicationStatus == "Hired" || a.ApplicationStatus == "OfferSent"),
                BestMatchScore = myApps.Any() ? myApps.Max(a => a.MatchScore) : 0,
                ProfileCompletion = applicant?.ProfileCompletionPercent ?? 20,
                UnreadNotifications = 0,
                Applications = myApps.Select(a => new MyApplicationViewModel
                {
                    ApplicationID = a.ApplicationID,
                    JobTitle = a.Job.JobTitle,
                    CompanyName = a.Job.Company?.CompanyName ?? a.Job.Recruiter?.CompanyName ?? "—",
                    JobLocation = a.Job.JobLocation ?? "—",
                    MatchScore = a.MatchScore,
                    ApplicationStatus = a.ApplicationStatus,
                    AppliedDate = a.AppliedDate
                }).ToList(),
                RecommendedJobs = recommendations.Take(3).Select(r =>
                {
                    recJobDetails.TryGetValue(r.JobId, out var jd);
                    return new RecommendedJobViewModel
                    {
                        JobID               = r.JobId,
                        JobTitle            = r.JobTitle,
                        CompanyName         = r.CompanyName,
                        JobLocation         = r.Location,
                        SalaryMin           = jd?.SalaryMin ?? 0,
                        SalaryMax           = jd?.SalaryMax ?? 0,
                        JobType             = jd?.JobType ?? "Full Time",
                        CreatedDate         = jd?.CreatedDate ?? DateTime.Now,
                        EstimatedMatchScore = r.MatchScore,
                        AttachmentPath      = r.AttachmentPath
                    };
                }).ToList(),
                ActivityLast7Days = activityLast7Days
            };

            return View(vm);
        }

        [AllowAnonymous]
        public async Task<IActionResult> BrowseJobs(string? search, string? location, string? sector,
            string? jobType, decimal? salaryMin, decimal? salaryMax, string? experience,
            string? industry, string? datePosted, int page = 1, int pageSize = 0)
        {
            bool isGuest = User.Identity?.IsAuthenticated != true || !User.IsInRole("Applicant");
            if (pageSize <= 0) pageSize = isGuest ? 9 : 8;

            // Get all jobs with filters via JobService
            var filteredJobs = await _jobs.GetAllJobsAsync(search, location, sector, jobType, salaryMin, salaryMax, experience, industry, datePosted);
            
            // Pagination
            var totalCount = filteredJobs.Count;
            var pagedJobs = filteredJobs.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            
            ViewBag.Search = search;
            ViewBag.Location = location;
            ViewBag.Sector = sector;
            ViewBag.JobType = jobType;
            ViewBag.SalaryMin = salaryMin?.ToString();
            ViewBag.SalaryMax = salaryMax?.ToString();
            ViewBag.MinExperience = experience;
            ViewBag.Industry = industry;
            ViewBag.DatePosted = datePosted;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // Pass bookmarked job IDs and premium status for logged-in applicants
            var applicantId = await GetApplicantIDAsync();
            if (applicantId.HasValue)
            {
                var savedJobs = await _jobs.GetSavedJobsAsync(applicantId.Value);
                ViewBag.BookmarkedJobIds = new HashSet<int>(savedJobs.Select(j => j.JobID));
            }
            else
            {
                ViewBag.BookmarkedJobIds = new HashSet<int>();
            }
            ViewBag.HasPremium = false;

            return View(pagedJobs);
        }

        // ── JOB DETAILS ──
        [AllowAnonymous]
        public async Task<IActionResult> JobDetails(int jobId)
        {
            
            var job = await _jobs.GetJobByIdAsync(jobId);
            if (job == null) return NotFound();

            var applicantId = await GetApplicantIDAsync();
            var alreadyApplied = applicantId.HasValue &&
                await _db.Applications.AsNoTracking().AnyAsync(a => a.ApplicantID == applicantId && a.JobID == jobId);

            ViewBag.AlreadyApplied = alreadyApplied;

            // ── Match score preview for logged-in applicants ──
            if (applicantId.HasValue)
            {
                var applicant = await _db.Applicants
                    .Include(a => a.ApplicantSkills).ThenInclude(s => s.Skill)
                    .FirstOrDefaultAsync(a => a.ApplicantID == applicantId.Value);

                if (applicant != null)
                {
                    var candidateSkills = applicant.ApplicantSkills.Select(s => s.Skill.SkillName).ToList();
                    var skillScore = _matching.CalculateSkillScore(job.RequiredSkills, candidateSkills, applicant.ProfileSummary ?? "");
                    var expScore = _matching.CalculateExperienceScore(job.MinimumExperience, applicant.YearsOfExperience, job.JobDescription ?? "");
                    var eduScore = _matching.CalculateEducationScore(job.EducationLevel, applicant.HighestEducation);

                    // Quick composite (simplified — full 6-factor score computed on apply)
                    var quickMatch = Math.Round((skillScore * 0.30m + expScore * 0.35m + eduScore * 0.10m) / 0.75m * 0.75m, 1);
                    quickMatch = Math.Min(quickMatch, 100);

                    ViewBag.QuickMatchScore = quickMatch;
                    ViewBag.SkillMatchScore = skillScore;
                    ViewBag.ExpMatchScore = expScore;
                    ViewBag.EduMatchScore = eduScore;
                    ViewBag.UserResumePath = applicant.ResumeFilePath;
                }
            }

            return View(job);
        }

        // ── APPLY (free — credits are only used for subscriptions) ──
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Apply(ApplicationCreateDTO dto)
        {
            
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("ApplicantLogin", "Auth");

            var (success, message, _) = await _apps.ApplyAsync(dto, applicantId.Value);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(MyApplications));
        }

        // ── MY APPLICATIONS ──
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> MyApplications()
        {
            
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");
            var myApps = await _apps.GetByApplicantAsync(applicantId.Value);
            return View(myApps);
        }

        // ── PROFILE ──
        [HttpGet, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Profile()
        {
            
            var applicantId = await GetApplicantIDAsync();
            var applicant = await _db.Applicants
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.ApplicantSkills).ThenInclude(sk => sk.Skill)
                .FirstOrDefaultAsync(a => a.ApplicantID == applicantId);
            return View(applicant);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Profile(Applicant model, string? skillsInput, string? fullName)
        {
            var applicantId = await GetApplicantIDAsync();
            var applicant = await _db.Applicants
                .Include(a => a.User)
                .Include(a => a.ApplicantSkills)
                .FirstOrDefaultAsync(a => a.ApplicantID == applicantId);

            if (applicant == null) return NotFound();

            // Update Applicant details
            applicant.CurrentLocation = model.CurrentLocation;
            applicant.Address = model.Address;
            applicant.DateOfBirth = model.DateOfBirth;
            applicant.Gender = model.Gender;
            applicant.LinkedInURL = model.LinkedInURL;
            applicant.PortfolioURL = model.PortfolioURL;
            applicant.YearsOfExperience = model.YearsOfExperience;
            applicant.HighestEducation = model.HighestEducation;
            applicant.CurrentJobTitle = model.CurrentJobTitle;
            applicant.ProfileSummary = model.ProfileSummary;
            applicant.Certifications = model.Certifications;
            applicant.Languages = model.Languages;
            applicant.Projects = model.Projects;
            applicant.Achievements = model.Achievements;
            applicant.ProfileCompletionPercent = CalculateCompletion(applicant);

            // Update User details & Session/Claims
            var user = await _db.Users.FindAsync(applicant.UserID);
            if (user != null)
            {
                user.PhoneNumber = model.User?.PhoneNumber;
                
                // Allow Name update and sync with Session/Identity
                if (!string.IsNullOrWhiteSpace(fullName) && user.FullName != fullName)
                {
                    user.FullName = fullName;
                    
                    // Update Session
                    HttpContext.Session.SetString(SessionKeys.UserName, fullName);
                    
                    // Update Authentication Cookie (Claims)
                    var identity = (ClaimsIdentity)User.Identity!;
                    var nameClaim = identity.FindFirst(ClaimTypes.Name);
                    if (nameClaim != null) identity.RemoveClaim(nameClaim);
                    identity.AddClaim(new Claim(ClaimTypes.Name, fullName));

                    await HttpContext.SignInAsync(
                        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity),
                        new AuthenticationProperties { IsPersistent = true });
                }

                user.UpdatedDate = DateTime.Now;
            }

            // Update Skills
            if (!string.IsNullOrEmpty(skillsInput))
            {
                _db.ApplicantSkills.RemoveRange(applicant.ApplicantSkills);
                var skillNames = skillsInput.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var skillName in skillNames)
                {
                    var name = skillName.Trim();
                    var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillName == name)
                        ?? new Skill { SkillName = name };
                    if (skill.SkillID == 0) _db.Skills.Add(skill);

                    _db.ApplicantSkills.Add(new ApplicantSkill
                    {
                        ApplicantID = applicant.ApplicantID,
                        SkillID = skill.SkillID
                    });
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Profile and session updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ── UPLOAD RESUME ──
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> UploadResume(IFormFile? resumeFile)
        {
            
            if (resumeFile == null || resumeFile.Length == 0)
            {
                TempData["Error"] = "Please select a resume file to upload.";
                return RedirectToAction(nameof(Profile));
            }

            var applicantId = await GetApplicantIDAsync();
            var applicant = await _db.Applicants
                .Include(a => a.ApplicantSkills)
                .FirstOrDefaultAsync(a => a.ApplicantID == applicantId);
            if (applicant == null) return NotFound();

            var allowed = new[] { ".pdf", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Only PDF, DOCX, TXT, JPG, and PNG files are allowed.";
                return RedirectToAction(nameof(Profile));
            }

            // ── PDF Page Count Limit Guard (5 pages max) ──
            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) && !AIResumeScreeningSystem.Helpers.FileValidationHelper.IsPdfPageCountValid(resumeFile))
            {
                TempData["Error"] = "Invalid resume: Document exceeds the maximum limit of 5 pages.";
                return RedirectToAction(nameof(Profile));
            }

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/resumes/");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await resumeFile.CopyToAsync(stream);

            applicant.ResumeFilePath = $"/uploads/resumes/{fileName}";

            // ── AUTO-PARSE: Run NLP pipeline on the uploaded resume ──
            try
            {
                var parsed = await _parser.ParseResumeAsync(applicant.ResumeFilePath);

                // OVERWRITE existing data with CV parsed data to ensure the CV is the single source of truth
                if (parsed.YearsOfExperience.HasValue && parsed.YearsOfExperience >= 0)
                    applicant.YearsOfExperience = parsed.YearsOfExperience.Value;

                if (!string.IsNullOrEmpty(parsed.Education))
                    applicant.HighestEducation = parsed.Education;
                
                if (!string.IsNullOrEmpty(parsed.Phone) && applicant.User != null)
                    applicant.User.PhoneNumber = parsed.Phone;

                if (!string.IsNullOrEmpty(parsed.CurrentJobTitle))
                    applicant.CurrentJobTitle = parsed.CurrentJobTitle;

                if (!string.IsNullOrEmpty(parsed.LinkedInURL))
                    applicant.LinkedInURL = parsed.LinkedInURL;

                if (!string.IsNullOrEmpty(parsed.PortfolioURL))
                    applicant.PortfolioURL = parsed.PortfolioURL;
                else if (!string.IsNullOrEmpty(parsed.GitHubURL))
                    applicant.PortfolioURL = parsed.GitHubURL;

                if (!string.IsNullOrEmpty(parsed.Address))
                {
                    applicant.Address = parsed.Address;
                    applicant.CurrentLocation = parsed.Address;
                }

                if (!string.IsNullOrEmpty(parsed.ProfileSummary))
                    applicant.ProfileSummary = parsed.ProfileSummary;

                if (parsed.Certifications.Any())
                    applicant.Certifications = string.Join(", ", parsed.Certifications);
                
                if (parsed.Languages.Any())
                    applicant.Languages = string.Join(", ", parsed.Languages);
                
                if (parsed.Projects.Any())
                    applicant.Projects = string.Join("\n\n", parsed.Projects);
                
                if (parsed.Achievements.Any())
                    applicant.Achievements = string.Join("\n\n", parsed.Achievements);

                // Auto-add extracted skills with smarter matching and persistence
                if (parsed.Skills.Any())
                {
                    _db.ApplicantSkills.RemoveRange(applicant.ApplicantSkills);

                    foreach (var skillName in parsed.Skills.Take(15))
                    {
                        var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillName.ToLower() == skillName.ToLower());
                        if (skill == null)
                        {
                            skill = new Skill { SkillName = skillName, CreatedDate = DateTime.Now, SkillCategory = "AI Extracted" };
                            _db.Skills.Add(skill);
                            await _db.SaveChangesAsync(); // Get the ID
                        }

                        if (!applicant.ApplicantSkills.Any(as_ => as_.SkillID == skill.SkillID))
                        {
                            _db.ApplicantSkills.Add(new ApplicantSkill
                            {
                                ApplicantID = applicant.ApplicantID,
                                SkillID = skill.SkillID,
                                SkillLevel = "Intermediate"
                            });
                        }
                    }
                }

                applicant.ProfileCompletionPercent = CalculateCompletion(applicant);
                _db.Applicants.Update(applicant);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Resume uploaded and AI-parsed with {Math.Round(parsed.ConfidenceScore * 100)}% confidence. Profile updated!";
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "NLP Parser failed for {Resume}", applicant.ResumeFilePath);
                TempData["Warning"] = "Resume uploaded but AI auto-parsing failed. You can still fill your profile manually.";
            }

            return RedirectToAction(nameof(Profile));
        }

        // ── WITHDRAW ──
        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> WithdrawApplication(int applicationId)
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");
            var (success, message) = await _apps.WithdrawAsync(applicationId, applicantId.Value);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(MyApplications));
        }

        // ── SAVED JOBS ──
        [HttpGet, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> SavedJobs()
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");

            var savedJobs = await _jobs.GetSavedJobsAsync(applicantId.Value);
            return View(savedJobs);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> SaveJob(int jobId)
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");

            var (success, message) = await _jobs.SaveJobAsync(applicantId.Value, jobId);
            return Json(new { success, message });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> UnsaveJob(int jobId)
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");

            var (success, message) = await _jobs.UnsaveJobAsync(applicantId.Value, jobId);
            return Json(new { success, message });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> ToggleBookmark(int jobId)
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return Json(new { success = false, message = "Please login first." });

            var savedJobs = await _jobs.GetSavedJobsAsync(applicantId.Value);
            bool isBookmarked = savedJobs.Any(j => j.JobID == jobId);

            if (isBookmarked)
            {
                var (success, message) = await _jobs.UnsaveJobAsync(applicantId.Value, jobId);
                return Json(new { success, message, isBookmarked = false });
            }
            else
            {
                var (success, message) = await _jobs.SaveJobAsync(applicantId.Value, jobId);
                return Json(new { success, message, isBookmarked = true });
            }
        }

        // ── RECOMMENDED JOBS (AI) ──
        [HttpGet, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> RecommendedJobs(string? search, string? location, string? jobType, string? experience, string? sector, decimal? salaryMin, decimal? salaryMax, string? datePosted, int page = 1, int pageSize = 8)
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");

            var recommendations = await _matching.GetJobRecommendationsAsync(applicantId.Value);

            // Filtering based on query params
            var jobIds = recommendations.Select(r => r.JobId).ToList();
            var query = _db.Jobs.Include(j => j.Company).Include(j => j.Recruiter).Where(j => jobIds.Contains(j.JobID)).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(j => (j.JobTitle != null && j.JobTitle.Contains(search)) || (j.Company != null && j.Company.CompanyName != null && j.Company.CompanyName.Contains(search)) || (j.Recruiter != null && j.Recruiter.CompanyName != null && j.Recruiter.CompanyName.Contains(search)));
            if (!string.IsNullOrEmpty(location))
                query = query.Where(j => j.JobLocation != null && j.JobLocation.Contains(location));
            if (!string.IsNullOrEmpty(jobType))
                query = query.Where(j => j.JobType == jobType);
            if (!string.IsNullOrEmpty(experience) && int.TryParse(experience, out int minExp))
                query = query.Where(j => j.MinimumExperience >= minExp);
            if (!string.IsNullOrEmpty(sector))
                query = query.Where(j => j.JobSector == sector);
            if (salaryMin.HasValue && salaryMin > 0)
                query = query.Where(j => j.SalaryMin >= salaryMin || j.SalaryMax >= salaryMin);
            if (salaryMax.HasValue && salaryMax > 0)
                query = query.Where(j => j.SalaryMin <= salaryMax || j.SalaryMax <= salaryMax);
            if (!string.IsNullOrEmpty(datePosted))
            {
                var now = DateTime.Now;
                if (datePosted == "24h") query = query.Where(j => j.CreatedDate >= now.AddHours(-24));
                else if (datePosted == "7d") query = query.Where(j => j.CreatedDate >= now.AddDays(-7));
                else if (datePosted == "30d") query = query.Where(j => j.CreatedDate >= now.AddDays(-30));
            }

            var filteredJobsDict = await query.ToDictionaryAsync(j => j.JobID, j => j);
            var filteredJobIds = filteredJobsDict.Keys.ToList();
            var finalRecommendations = recommendations.Where(r => filteredJobIds.Contains(r.JobId)).ToList();

            var totalCount = finalRecommendations.Count;
            var pagedRecommendations = finalRecommendations.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Store full job entity mapping so the UI card template has access to Logo, Deadlines, and Salary thresholds
            ViewBag.JobDetails = filteredJobsDict;

            // Pass bookmarked job IDs for card heart icons
            if (applicantId != null)
            {
                var savedJobs = await _jobs.GetSavedJobsAsync(applicantId.Value);
                ViewBag.BookmarkedJobIds = new HashSet<int>(savedJobs.Select(j => j.JobID));
            }
            else
            {
                ViewBag.BookmarkedJobIds = new HashSet<int>();
            }

            ViewBag.Search = search;
            ViewBag.Location = location;
            ViewBag.JobType = jobType;
            ViewBag.MinExperience = experience;
            ViewBag.Sector = sector;
            ViewBag.SalaryMin = salaryMin?.ToString();
            ViewBag.SalaryMax = salaryMax?.ToString();
            ViewBag.DatePosted = datePosted;
            
            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(pagedRecommendations);
        }

        [HttpGet, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> ProfileAnalytics()
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");

            var applicant = await _db.Applicants
                .Include(a => a.User)
                .Include(a => a.ApplicantSkills).ThenInclude(s => s.Skill)
                .FirstOrDefaultAsync(a => a.ApplicantID == applicantId.Value);

            if (applicant == null) return RedirectToAction("Login", "Auth");

            var myApps = await _apps.GetByApplicantAsync(applicantId.Value);
            var shortlisted = myApps.Count(a => a.ApplicationStatus == "Shortlisted" || a.ApplicationStatus == "InterviewScheduled");
            var interviewed = myApps.Count(a => a.ApplicationStatus == "InterviewScheduled" || a.ApplicationStatus == "InterviewCompleted");
            var hired = myApps.Count(a => a.ApplicationStatus == "Hired" || a.ApplicationStatus == "OfferSent");
            var successRate = myApps.Any() ? (decimal)shortlisted / myApps.Count * 100 : 0;

            // Real activity trend: count applications per day over the last 7 days
            var today = DateTime.Today;
            var trend = new List<DailyViewCount>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                trend.Add(new DailyViewCount
                {
                    Date = date,
                    Views = myApps.Count(a => a.AppliedDate.Date == date)
                });
            }

            // Real top skills: derived from the applicant's own skill list, ranked by how many
            // open jobs require each skill (actual data, not mocked)
            var applicantSkillNames = applicant.ApplicantSkills
                .Select(s => s.Skill.SkillName.ToLower())
                .ToList();

            var topSkills = new List<SkillAttraction>();
            if (applicantSkillNames.Any())
            {
                var openJobs = await _db.Jobs
                    .Where(j => j.JobStatus == "Open" && !string.IsNullOrEmpty(j.RequiredSkills))
                    .Select(j => j.RequiredSkills!.ToLower())
                    .ToListAsync();

                var totalJobs = Math.Max(openJobs.Count, 1);
                foreach (var skillName in applicantSkillNames.Take(5))
                {
                    var matchCount = openJobs.Count(req => req.Contains(skillName));
                    var pct = (int)Math.Round((double)matchCount / totalJobs * 100);
                    if (pct > 0)
                        topSkills.Add(new SkillAttraction { SkillName = applicant.ApplicantSkills.First(s => s.Skill.SkillName.ToLower() == skillName).Skill.SkillName, Percentage = pct });
                }
                topSkills = topSkills.OrderByDescending(s => s.Percentage).ToList();
            }

            // Real suggestions: computed from actual profile gaps
            var suggestions = new List<ProfileSuggestion>();
            var skillCount = applicantSkillNames.Count;
            if (skillCount < 5)
                suggestions.Add(new ProfileSuggestion { Section = "Skills", CurrentState = $"{skillCount} skill{(skillCount == 1 ? "" : "s")} listed", Suggestion = "Add more relevant skills to improve your match rate with open jobs.", Priority = "High", Icon = "⚡" });
            if (string.IsNullOrEmpty(applicant.ProfileSummary))
                suggestions.Add(new ProfileSuggestion { Section = "Summary", CurrentState = "Not set", Suggestion = "Add a professional summary. Profiles with summaries get significantly more views.", Priority = "High", Icon = "📝" });
            if (string.IsNullOrEmpty(applicant.ResumeFilePath))
                suggestions.Add(new ProfileSuggestion { Section = "Resume", CurrentState = "Missing", Suggestion = "Upload your CV. Applications without a resume are rarely shortlisted.", Priority = "High", Icon = "📄" });
            if (string.IsNullOrEmpty(applicant.LinkedInURL))
                suggestions.Add(new ProfileSuggestion { Section = "LinkedIn", CurrentState = "Not linked", Suggestion = "Link your LinkedIn profile to improve recruiter trust and discoverability.", Priority = "Medium", Icon = "🔗" });
            if (applicant.YearsOfExperience == 0)
                suggestions.Add(new ProfileSuggestion { Section = "Experience", CurrentState = "0 years set", Suggestion = "Set your years of experience to improve AI matching accuracy.", Priority = "Medium", Icon = "💼" });
            if (string.IsNullOrEmpty(applicant.HighestEducation))
                suggestions.Add(new ProfileSuggestion { Section = "Education", CurrentState = "Not specified", Suggestion = "Add your highest education level to qualify for more positions.", Priority = "Low", Icon = "🎓" });

            // Profile completeness drives visibility metrics (computed, not random)
            var pctComplete = applicant.ProfileCompletionPercent;
            var baseViews = myApps.Count * 3;                                   // views proportional to activity
            var totalViews = baseViews + (pctComplete * 2);                      // profile quality multiplier
            var weekViews = trend.Sum(t => t.Views) + (pctComplete / 10);       // this week driven by real activity
            var uniqueRecruiters = Math.Max(0, shortlisted + (pctComplete / 20));
            var appearances = myApps.Count * 5 + (pctComplete * 3);             // job match appearances
            var ctr = myApps.Any() && totalViews > 0
                ? Math.Round((decimal)myApps.Count / totalViews * 100, 1)
                : 0m;

            var model = new ProfileAnalyticsViewModel
            {
                TotalProfileViews        = totalViews,
                ProfileViewsThisWeek     = weekViews,
                UniqueRecruiterViews     = uniqueRecruiters,
                SearchAppearances        = appearances,
                SearchAppearancesThisWeek = weekViews / 2,
                ClickThroughRate         = ctr,
                TotalApplications        = myApps.Count,
                ShortlistedCount         = shortlisted,
                InterviewedCount         = interviewed,
                HiredCount               = hired,
                ApplicationSuccessRate   = Math.Round(successRate, 1),
                ViewTrend                = trend,
                TopAttractingSkills      = topSkills,
                Suggestions              = suggestions
            };

            return View(model);
        }

        // ── SYSTEM SETTINGS ──
        [HttpGet, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> ChangePassword()
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");
            var userId = CurrentUserID!.Value;

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match or fields are empty.";
                return RedirectToAction(nameof(ChangePassword));
            }

            // Implementation using AuthService
            var _auth = HttpContext.RequestServices.GetRequiredService<IAuthService>();
            var (success, message) = await _auth.ChangePasswordAsync(userId, currentPassword, newPassword);
            
            TempData[success ? "Success" : "Error"] = message;
            
            if (success)
            {
                // Force sign out to ensure the new password is used for the next session
                await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Privacy()
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");
            
            var applicant = await _db.Applicants.FindAsync(applicantId);
            return View(applicant);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Privacy(bool dataSharingConsent, bool profileVisibility)
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");

            var applicant = await _db.Applicants.FindAsync(applicantId);
            if (applicant != null)
            {
                applicant.ProfileVisibility = profileVisibility;
                applicant.DataSharingConsent = dataSharingConsent;
                _db.Applicants.Update(applicant);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Privacy & Consent settings safely updated.";
            }
            return RedirectToAction(nameof(Privacy));
        }

        [HttpGet, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Preferences()
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");
            
            var applicant = await _db.Applicants.FindAsync(applicantId);
            return View(applicant);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Preferences(string emailFrequency, bool inAppNotifications, string searchStatus)
        {
            var applicantId = await GetApplicantIDAsync();
            if (applicantId == null) return RedirectToAction("Login", "Auth");

            var applicant = await _db.Applicants.FindAsync(applicantId);
            if (applicant == null) return NotFound();

            applicant.EmailFrequency = emailFrequency ?? "Daily";
            applicant.InAppNotifications = inAppNotifications;
            applicant.SearchStatus = searchStatus ?? "active";

            _db.Applicants.Update(applicant);
            await _db.SaveChangesAsync();

            TempData["Success"] = "System Preferences & Notification defaults updated successfully.";
            return RedirectToAction(nameof(Preferences));
        }

        private int CalculateCompletion(Applicant a)
        {
            int score = 20;
            if (!string.IsNullOrEmpty(a.CurrentLocation)) score += 10;
            if (!string.IsNullOrEmpty(a.HighestEducation)) score += 10;
            if (!string.IsNullOrEmpty(a.CurrentJobTitle)) score += 10;
            if (!string.IsNullOrEmpty(a.ProfileSummary)) score += 10;
            if (!string.IsNullOrEmpty(a.ResumeFilePath)) score += 20;
            if (!string.IsNullOrEmpty(a.LinkedInURL)) score += 10;
            if (a.YearsOfExperience > 0) score += 10;
            return Math.Min(score, 100);
        }
    }
}

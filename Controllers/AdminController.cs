using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.Services;
using Microsoft.AspNetCore.Authorization;

namespace AIResumeScreeningSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("portal/admin/[action]")]
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly AdminManagementService _adminService;

        public AdminController(ApplicationDbContext db, AdminManagementService adminService)
        {
            _db = db;
            _adminService = adminService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────────────────────────────
        [Route("~/portal/admin")]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var overview = await _adminService.GetSystemOverviewAsync();

            var totalEarnings     = await _db.CreditTransactions.Where(t => t.ActionType == "PURCHASE").SumAsync(t => t.AmountBDT);
            var earningsThisMonth = await _db.CreditTransactions.Where(t => t.ActionType == "PURCHASE" && t.CreatedAt >= DateTime.Now.AddDays(-30)).SumAsync(t => t.AmountBDT);

            var recentUsers = await _db.Users
                .OrderByDescending(u => u.CreatedDate)
                .Take(6)
                .ToListAsync();

            var recentTransactions = await _db.CreditTransactions
                .Include(t => t.User)
                .Where(t => t.ActionType == "PURCHASE")
                .OrderByDescending(t => t.CreatedAt)
                .Take(6)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers          = overview.TotalUsers,
                TotalRecruiters     = overview.TotalRecruiters,
                TotalApplicants     = overview.TotalApplicants,
                ActiveJobs          = overview.ActiveJobs,
                TotalApplications   = overview.TotalApplications,
                TotalHired          = overview.TotalHired,
                ScheduledInterviews = overview.TotalInterviews,
                NewUsersThisMonth   = await _db.Users.CountAsync(u => u.CreatedDate >= DateTime.Now.AddDays(-30)),
                AverageMatchScore   = overview.AverageMatchScore,
                ConversionRate      = overview.ConversionRate,
                TotalEarnings       = totalEarnings,
                EarningsThisMonth   = earningsThisMonth,
                RecentUsers         = recentUsers,
                RecentTransactions  = recentTransactions
            };

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────────
        // USER MANAGEMENT
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Recruiters()
        {
            var recruiters = await _db.Users
                .Where(u => u.Role == "Recruiter")
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();
            return View(recruiters);
        }

        public async Task<IActionResult> Applicants()
        {
            var applicants = await _db.Users
                .Where(u => u.Role == "Applicant")
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();
            return View(applicants);
        }

        [HttpGet]
        public IActionResult CreateRecruiter()
        {
            ViewBag.Companies = _db.Companies.OrderBy(c => c.CompanyName).ToList();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRecruiter(CreateRecruiterDTO dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Companies = _db.Companies.OrderBy(c => c.CompanyName).ToList();
                return View(dto);
            }

            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                ViewBag.Companies = _db.Companies.OrderBy(c => c.CompanyName).ToList();
                return View(dto);
            }

            if (dto.CompanyID == null && string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                ModelState.AddModelError("CompanyID", "Please select an existing company or enter a new company name.");
                ViewBag.Companies = _db.Companies.OrderBy(c => c.CompanyName).ToList();
                return View(dto);
            }

            var user = new User
            {
                FullName      = dto.FullName,
                Email         = dto.Email,
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role          = "Recruiter",
                AccountStatus = "Active",
                EmailVerified = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var recruiter = new Recruiter
            {
                UserID          = user.UserID,
                CompanyID       = dto.CompanyID,
                CompanyName     = dto.CompanyName,
                Designation     = dto.Designation,
                RecruiterStatus = "Active"
            };

            // If a new company name was provided but no company selected, create the company
            if (dto.CompanyID == null && !string.IsNullOrWhiteSpace(dto.CompanyName))
            {
                var newCompany = new Company
                {
                    CompanyName = dto.CompanyName,
                    CompanyStatus = "Active"
                };
                _db.Companies.Add(newCompany);
                await _db.SaveChangesAsync();

                recruiter.CompanyID = newCompany.CompanyID;
            }

            _db.Recruiters.Add(recruiter);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Recruiter account for {user.FullName} created successfully.";
            return RedirectToAction(nameof(Recruiters));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Prevent disabling the last active admin
            if (user.Role == "Admin" && user.AccountStatus == "Active")
            {
                var activeAdmins = await _db.Users.CountAsync(u => u.Role == "Admin" && u.AccountStatus == "Active");
                if (activeAdmins <= 1)
                {
                    TempData["Error"] = "Cannot disable the last active admin account.";
                    return RedirectToAction(user.Role == "Recruiter" ? nameof(Recruiters) : nameof(Applicants));
                }
            }

            user.AccountStatus = user.AccountStatus == "Active" ? "Disabled" : "Active";
            user.UpdatedDate   = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{user.FullName} is now {user.AccountStatus}.";
            return RedirectToAction(user.Role == "Recruiter" ? nameof(Recruiters) : nameof(Applicants));
        }

        // ─────────────────────────────────────────────────────────────────────
        // ANALYTICS
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Analytics()
        {
            var allScores = await _db.Applications.Select(a => a.MatchScore).ToListAsync();

            var model = new AdminAnalyticsViewModel
            {
                TotalHired        = await _db.Applications.CountAsync(a => a.ApplicationStatus == "Hired"),
                AvgAIScore        = allScores.Any() ? Math.Round(allScores.Average(s => (double)s), 1) : 0,
                TotalApplications = await _db.Applications.CountAsync(),

                AppsByStatus = await _db.Applications
                    .GroupBy(a => a.ApplicationStatus)
                    .Select(g => new AppStatusCount { Status = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(),

                ScoreDistribution = new List<ScoreBandCount>
                {
                    new ScoreBandCount { Range = "75–100 (Excellent)", Count = allScores.Count(s => s >= 75) },
                    new ScoreBandCount { Range = "50–74 (Good)",       Count = allScores.Count(s => s >= 50 && s < 75) },
                    new ScoreBandCount { Range = "25–49 (Fair)",       Count = allScores.Count(s => s >= 25 && s < 50) },
                    new ScoreBandCount { Range = "0–24 (Low)",         Count = allScores.Count(s => s < 25) }
                },

                TopJobs = await _db.Jobs
                    .Where(j => j.Applications.Any())
                    .OrderByDescending(j => j.Applications.Count)
                    .Take(8)
                    .Select(j => new TopJobAnalytic
                    {
                        JobTitle = j.JobTitle,
                        Count    = j.Applications.Count,
                        AvgScore = j.Applications.Any() ? Math.Round(j.Applications.Average(a => (double)a.MatchScore), 1) : 0
                    })
                    .ToListAsync(),

                TopSkills = await _db.Skills
                    .Where(s => s.ApplicantSkills.Any())
                    .OrderByDescending(s => s.ApplicantSkills.Count)
                    .Take(10)
                    .Select(s => new SkillDemandCount { SkillName = s.SkillName, Count = s.ApplicantSkills.Count })
                    .ToListAsync()
            };

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────────
        // REVENUE
        // ─────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Revenue()
        {
            var transactions = await _db.CreditTransactions
                .Include(t => t.User)
                .Where(t => t.ActionType == "PURCHASE")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.TotalRevenue      = transactions.Sum(t => t.AmountBDT);
            ViewBag.ThisMonthRevenue  = transactions.Where(t => t.CreatedAt >= DateTime.Now.AddDays(-30)).Sum(t => t.AmountBDT);
            ViewBag.TotalTransactions = transactions.Count;

            return View(transactions);
        }


    }
}

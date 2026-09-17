using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AIResumeScreeningSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public class AdminApiController : BaseApiController
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminApiController> _logger;

        public AdminApiController(ApplicationDbContext db, ILogger<AdminApiController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _db.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(u => u.AccountStatus == status);

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    userId = u.UserID,
                    email = u.Email,
                    fullName = u.FullName,
                    role = u.Role,
                    status = u.AccountStatus,
                    createdDate = u.CreatedDate,
                    lastLogin = u.LastLoginDate
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new { totalCount, page, pageSize, users }
            });
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            object? additionalData = null;

            if (user.Role == "Applicant")
            {
                var applicant = await _db.Applicants
                    .Include(a => a.ApplicantSkills).ThenInclude(s => s.Skill)
                    .FirstOrDefaultAsync(a => a.UserID == id);
                
                if (applicant != null)
                {
                    additionalData = new
                    {
                        yearsOfExperience = applicant.YearsOfExperience,
                        highestEducation = applicant.HighestEducation,
                        profileCompletion = applicant.ProfileCompletionPercent,
                        skills = applicant.ApplicantSkills.Select(s => s.Skill.SkillName)
                    };
                }
            }
            else if (user.Role == "Recruiter")
            {
                var recruiter = await _db.Recruiters
                    .Include(r => r.Company)
                    .FirstOrDefaultAsync(r => r.UserID == id);
                
                if (recruiter != null)
                {
                    additionalData = new
                    {
                        designation = recruiter.Designation,
                        companyName = recruiter.Company?.CompanyName
                    };
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId = user.UserID,
                    email = user.Email,
                    fullName = user.FullName,
                    phone = user.PhoneNumber,
                    role = user.Role,
                    status = user.AccountStatus,
                    createdDate = user.CreatedDate,
                    lastLogin = user.LastLoginDate,
                    additionalData
                }
            });
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest model)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            var oldRole = user.Role;
            user.Role = model.Role;
            user.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} role changed from {OldRole} to {NewRole}", id, oldRole, model.Role);

            return Ok(new { success = true, message = $"Role updated from {oldRole} to {model.Role}" });
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusRequest model)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            user.AccountStatus = model.Status;
            user.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = $"Status updated to {model.Status}" });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalApplicants = await _db.Users.CountAsync(u => u.Role == "Applicant");
            var totalRecruiters = await _db.Users.CountAsync(u => u.Role == "Recruiter");
            var activeUsers = await _db.Users.CountAsync(u => u.AccountStatus == "Active");

            var totalJobs = await _db.Jobs.CountAsync();
            var openJobs = await _db.Jobs.CountAsync(j => j.JobStatus == "Open");
            var closedJobs = await _db.Jobs.CountAsync(j => j.JobStatus == "Closed");

            var totalApplications = await _db.Applications.CountAsync();
            var pendingApps = await _db.Applications.CountAsync(a => a.ApplicationStatus == "Applied");
            var shortlistedApps = await _db.Applications.CountAsync(a => a.ApplicationStatus == "Shortlisted");
            var interviewApps = await _db.Applications.CountAsync(a => a.ApplicationStatus == "InterviewScheduled");
            var hiredApps = await _db.Applications.CountAsync(a => a.ApplicationStatus == "OfferAccepted");

            var avgScore = await _db.Applications
                .Where(a => a.MatchScore > 0)
                .Select(a => (decimal?)a.MatchScore)
                .AverageAsync() ?? 0m;

            return Ok(new
            {
                success = true,
                data = new
                {
                    users = new
                    {
                        total = totalUsers,
                        applicants = totalApplicants,
                        recruiters = totalRecruiters,
                        active = activeUsers
                    },
                    jobs = new
                    {
                        total = totalJobs,
                        open = openJobs,
                        closed = closedJobs
                    },
                    applications = new
                    {
                        total = totalApplications,
                        pending = pendingApps,
                        shortlisted = shortlistedApps,
                        interview = interviewApps,
                        hired = hiredApps,
                        averageMatchScore = Math.Round(avgScore, 2)
                    }
                }
            });
        }

        [HttpGet("logs")]
        public Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            return Task.FromResult<IActionResult>(Ok(new
            {
                success = true,
                data = new { totalCount = 0, page, pageSize, logs = new List<object>() }
            }));
        }

        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies([FromQuery] string? status)
        {
            var query = _db.Companies.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.CompanyStatus == status);

            var companies = await query
                .Include(c => c.Recruiters)
                .Include(c => c.Jobs)
                .Select(c => new
                {
                    companyId = c.CompanyID,
                    name = c.CompanyName,
                    website = c.CompanyWebsite,
                    location = c.CompanyLocation,
                    size = c.CompanySize,
                    industry = c.IndustryType,
                    status = c.CompanyStatus,
                    recruiterCount = c.Recruiters.Count,
                    jobCount = c.Jobs.Count,
                    createdDate = c.CreatedDate
                })
                .ToListAsync();

            return Ok(new { success = true, data = companies });
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetSystemHealth()
        {
            var dbConnection = _db.Database.CanConnect();
            
            var dbSize = "Unknown";
            try
            {
                var result = await _db.Database.SqlQueryRaw<string>("SELECT CONCAT(DB_NAME(), ' - ', CAST(SUM(size) * 8 / 1024 AS VARCHAR), ' MB') FROM sys.master_files WHERE DB_NAME() = DB_NAME()").FirstOrDefaultAsync();
                dbSize = result ?? "Unknown";
            }
            catch { }

            return Ok(new
            {
                success = true,
                data = new
                {
                    status = "Healthy",
                    database = new
                    {
                        connected = dbConnection,
                        size = dbSize
                    },
                    timestamp = DateTime.UtcNow,
                    version = "2.0"
                }
            });
        }
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
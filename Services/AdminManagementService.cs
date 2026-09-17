using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.Models;
using System.Diagnostics;

namespace AIResumeScreeningSystem.Services
{
    public class AdminManagementService
    {
        private readonly ApplicationDbContext _db;

        public AdminManagementService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<SystemOverview> GetSystemOverviewAsync()
        {
            var sw = Stopwatch.StartNew();
            
            var usersTask = await _db.Users.CountAsync();
            var recruitersTask = await _db.Recruiters.CountAsync();
            var applicantsTask = await _db.Applicants.CountAsync();
            var companiesTask = await _db.Companies.CountAsync();
            var jobsTask = await _db.Jobs.CountAsync();
            var activeJobsTask = await _db.Jobs.CountAsync(j => j.JobStatus == "Open");
            var appsTask = await _db.Applications.CountAsync();
            var hiredTask = await _db.Applications.CountAsync(a => a.ApplicationStatus == "Hired");
            var interviewsTask = await _db.Interviews.CountAsync();
            var offersTask = 0;
            var matchesTask = 0;
            var avgScoreTask = await _db.Applications.Where(a => a.MatchScore > 0).Select(a => (double?)a.MatchScore).AverageAsync();

            var overview = new SystemOverview
            {
                TotalUsers = usersTask,
                TotalRecruiters = recruitersTask,
                TotalApplicants = applicantsTask,
                TotalCompanies = companiesTask,
                TotalJobs = jobsTask,
                ActiveJobs = activeJobsTask,
                TotalApplications = appsTask,
                TotalHired = hiredTask,
                TotalInterviews = interviewsTask,
                TotalOffers = offersTask,
                ProcessedMatches = matchesTask,
                AverageMatchScore = avgScoreTask.HasValue ? Math.Round(avgScoreTask.Value, 1) : 0,
                ConversionRate = appsTask > 0 ? Math.Round((double)hiredTask / appsTask * 100, 1) : 0,
                LastUpdated = DateTime.Now
            };

            sw.Stop();
            return overview;
        }

        public async Task<UserStatistics> GetUserStatisticsAsync()
        {
            var stats = new UserStatistics
            {
                TotalUsers = await _db.Users.CountAsync(),
                ActiveUsers = await _db.Users.CountAsync(u => u.AccountStatus == "Active"),
                DisabledUsers = await _db.Users.CountAsync(u => u.AccountStatus == "Disabled"),
                RecruitersCount = await _db.Recruiters.CountAsync(r => r.RecruiterStatus == "Active"),
                ApplicantsCount = await _db.Applicants.CountAsync(),
                NewUsersThisMonth = await _db.Users.CountAsync(u => u.CreatedDate >= DateTime.Now.AddDays(-30)),
                UsersByRole = await _db.Users.GroupBy(u => u.Role).Select(g => new RoleCount { Role = g.Key, Count = g.Count() }).ToListAsync()
            };

            return stats;
        }


        public async Task<JobMarketAnalysis> GetJobMarketAnalysisAsync()
        {
            var totalJobs = await _db.Jobs.CountAsync();
            var activeJobs = await _db.Jobs.CountAsync(j => j.JobStatus == "Open");
            var closedJobs = await _db.Jobs.CountAsync(j => j.JobStatus == "Closed");
            var avgAppsPerJob = await _db.Jobs.Select(j => (double?)j.Applications.Count).AverageAsync();
            var topJobTitles = await _db.Jobs.GroupBy(j => j.JobTitle).OrderByDescending(g => g.Count()).Take(10).Select(g => new JobTitleCount { Title = g.Key, Count = g.Count() }).ToListAsync();
            var topLocations = await _db.Jobs.Where(j => !string.IsNullOrEmpty(j.JobLocation)).GroupBy(j => j.JobLocation!).OrderByDescending(g => g.Count()).Take(5).Select(g => new LocationCount { Location = g.Key, Count = g.Count() }).ToListAsync();
            var jobTypes = await _db.Jobs.GroupBy(j => j.JobType).Select(g => new JobTypeCount { Type = g.Key, Count = g.Count() }).ToListAsync();
            
            return new JobMarketAnalysis
            {
                TotalJobPostings = totalJobs,
                ActiveJobPostings = activeJobs,
                ClosedJobPostings = closedJobs,
                AverageApplicationsPerJob = avgAppsPerJob.HasValue ? Math.Round(avgAppsPerJob.Value, 1) : 0,
                TopJobTitles = topJobTitles,
                TopLocations = topLocations,
                JobTypesDistribution = jobTypes
            };
        }

        public async Task<List<SystemHealthMetric>> GetSystemHealthAsync()
        {
            var metrics = new List<SystemHealthMetric>();

            var dbSize = await GetDatabaseSizeAsync();
            metrics.Add(new SystemHealthMetric { MetricName = "Database Size", Value = dbSize, Status = "Good", Icon = "database" });

            var userCount = await _db.Users.CountAsync();
            metrics.Add(new SystemHealthMetric { MetricName = "Total Users", Value = userCount.ToString(), Status = userCount > 0 ? "Good" : "Warning", Icon = "users" });

            var activeJobs = await _db.Jobs.CountAsync(j => j.JobStatus == "Open");
            metrics.Add(new SystemHealthMetric { MetricName = "Active Jobs", Value = activeJobs.ToString(), Status = activeJobs > 0 ? "Good" : "Info", Icon = "briefcase" });

            var recentApps = await _db.Applications.CountAsync(a => a.AppliedDate >= DateTime.Now.AddDays(-7));
            metrics.Add(new SystemHealthMetric { MetricName = "Applications (7 days)", Value = recentApps.ToString(), Status = recentApps > 0 ? "Good" : "Info", Icon = "file-text" });

            var pendingApps = await _db.Applications.CountAsync(a => a.ApplicationStatus == "Applied");
            metrics.Add(new SystemHealthMetric { MetricName = "Pending Applications", Value = pendingApps.ToString(), Status = pendingApps == 0 ? "Good" : "Info", Icon = "send" });

            return metrics;
        }

        private async Task<string> GetDatabaseSizeAsync()
        {
            try
            {
                var connection = _db.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT CAST(SUM(size) * 8.0 / 1024 AS VARCHAR(20)) + ' MB' FROM sys.database_files";
                var result = await command.ExecuteScalarAsync();
                return result?.ToString() ?? "Unknown";
            }
            catch
            {
                return "N/A";
            }
        }

        public async Task<BulkUserActionResult> BulkUpdateUserStatusAsync(List<int> userIds, string newStatus)
        {
            var users = await _db.Users.Where(u => userIds.Contains(u.UserID)).ToListAsync();
            var updated = 0;

            foreach (var user in users)
            {
                user.AccountStatus = newStatus;
                user.UpdatedDate = DateTime.Now;
                updated++;
            }

            await _db.SaveChangesAsync();

            return new BulkUserActionResult
            {
                Success = true,
                AffectedCount = updated,
                Message = $"{updated} user(s) status updated to {newStatus}"
            };
        }

        public async Task<BulkUserActionResult> BulkDeleteUsersAsync(List<int> userIds)
        {
            var users = await _db.Users.Where(u => userIds.Contains(u.UserID)).ToListAsync();
            
            var adminUsers = users.Where(u => u.Role == "Admin").ToList();
            if (adminUsers.Any())
            {
                return new BulkUserActionResult
                {
                    Success = false,
                    AffectedCount = 0,
                    Message = "Cannot delete admin users. Please change their role first."
                };
            }
            
            var applicants = await _db.Applicants.Where(a => userIds.Contains(a.UserID)).ToListAsync();
            var recruiters = await _db.Recruiters.Where(r => userIds.Contains(r.UserID)).ToListAsync();

            _db.Applicants.RemoveRange(applicants);
            _db.Recruiters.RemoveRange(recruiters);
            _db.Users.RemoveRange(users);

            await _db.SaveChangesAsync();

            return new BulkUserActionResult
            {
                Success = true,
                AffectedCount = users.Count,
                Message = $"{users.Count} user(s) permanently deleted"
            };
        }

        public async Task<AdminUserProfile> GetUserDetailsAsync(int userId)
        {
            var user = await _db.Users
                .Include(u => u.Applicant)
                    .ThenInclude(a => a!.Applications).ThenInclude(app => app.Job)
                .Include(u => u.Recruiter)
                    .ThenInclude(r => r!.Jobs)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return new AdminUserProfile();

            return new AdminUserProfile
            {
                UserId = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                AccountStatus = user.AccountStatus,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                EmailVerified = user.EmailVerified,
                PhoneNumber = user.PhoneNumber,
                IsApplicant = user.Applicant != null,
                IsRecruiter = user.Recruiter != null,
                ApplicationCount = user.Applicant?.Applications.Count ?? 0,
                JobCount = user.Recruiter?.Jobs.Count ?? 0,
                NotificationCount = 0,
                ActivityLogCount = 0
            };
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole)
        {
            var allowedRoles = new[] { "Admin", "Recruiter", "Applicant" };
            if (!allowedRoles.Contains(newRole)) return false;

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            var oldRole = user.Role;
            user.Role = newRole;
            user.UpdatedDate = DateTime.Now;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<SystemConfiguration> GetSystemConfigurationAsync()
        {
            return new SystemConfiguration
            {
                Settings = new Dictionary<string, string>(),
                Labels = new Dictionary<string, string>(),
                LastUpdated = DateTime.Now
            };
        }

        public Task<bool> UpdateSystemSettingAsync(string key, string value)
        {
            // Settings table removed - no-op
            return Task.FromResult(true);
        }

        public Task<bool> UpdateSettingsAsync(Dictionary<string, string> settings)
        {
            // Settings table removed - no-op
            return Task.FromResult(true);
        }
    }

    public class SystemOverview
    {
        public int TotalUsers { get; set; }
        public int TotalRecruiters { get; set; }
        public int TotalApplicants { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int TotalApplications { get; set; }
        public int TotalHired { get; set; }
        public int TotalInterviews { get; set; }
        public int TotalOffers { get; set; }
        public int ProcessedMatches { get; set; }
        public double ConversionRate { get; set; }
        public double AverageMatchScore { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int DisabledUsers { get; set; }
        public int RecruitersCount { get; set; }
        public int ApplicantsCount { get; set; }
        public int NewUsersThisMonth { get; set; }
        public List<RoleCount> UsersByRole { get; set; } = new();
    }

    public class RoleCount
    {
        public string Role { get; set; } = "";
        public int Count { get; set; }
    }


    public class CompanyHiringStats
    {
        public string CompanyName { get; set; } = "";
        public int JobsPosted { get; set; }
        public int TotalApplications { get; set; }
        public int HiredCount { get; set; }
    }

    public class JobMarketAnalysis
    {
        public int TotalJobPostings { get; set; }
        public int ActiveJobPostings { get; set; }
        public int ClosedJobPostings { get; set; }
        public double AverageApplicationsPerJob { get; set; }
        public List<JobTitleCount> TopJobTitles { get; set; } = new();
        public List<LocationCount> TopLocations { get; set; } = new();
        public List<JobTypeCount> JobTypesDistribution { get; set; } = new();
    }

    public class JobTitleCount { public string Title { get; set; } = ""; public int Count { get; set; } }
    public class LocationCount { public string Location { get; set; } = ""; public int Count { get; set; } }
    public class JobTypeCount { public string Type { get; set; } = ""; public int Count { get; set; } }

    public class SystemHealthMetric
    {
        public string MetricName { get; set; } = "";
        public string Value { get; set; } = "";
        public string Status { get; set; } = "Good";
        public string Icon { get; set; } = "";
    }

    public class BulkUserActionResult
    {
        public bool Success { get; set; }
        public int AffectedCount { get; set; }
        public string Message { get; set; } = "";
    }

    public class AdminUserProfile
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string AccountStatus { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool EmailVerified { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsApplicant { get; set; }
        public bool IsRecruiter { get; set; }
        public int ApplicationCount { get; set; }
        public int JobCount { get; set; }
        public int NotificationCount { get; set; }
        public int ActivityLogCount { get; set; }
    }

    public class SystemConfiguration
    {
        public Dictionary<string, string> Settings { get; set; } = new();
        public Dictionary<string, string> Labels { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
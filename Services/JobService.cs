using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using AIResumeScreeningSystem.Data;
using AIResumeScreeningSystem.DTOs;
using AIResumeScreeningSystem.Interfaces;
using AIResumeScreeningSystem.Models;

namespace AIResumeScreeningSystem.Services
{
    public class JobService : IJobService
    {
        private readonly ApplicationDbContext _db;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public JobService(ApplicationDbContext db, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<List<Job>> GetAllJobsAsync(string? search = null, string? location = null, string? sector = null)
        {
            return await GetAllJobsAsync(search, location, sector, null, null, null, null, null, null);
        }

        public async Task<List<Job>> GetAllJobsAsync(string? search = null, string? location = null, string? sector = null,
            string? jobType = null, decimal? salaryMin = null, decimal? salaryMax = null,
            string? experience = null, string? industry = null, string? datePosted = null)
        {
            var query = _db.Jobs
                .TagWith("JobService.GetAllJobsAsync")
                .Include(j => j.Recruiter).ThenInclude(r => r.User)
                .Include(j => j.Company)
                .AsNoTracking()
                .Where(j => j.JobStatus == "Open")
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(j => j.JobTitle.ToLower().Contains(s) 
                    || (j.RequiredSkills != null && j.RequiredSkills.ToLower().Contains(s))
                    || (j.JobLocation != null && j.JobLocation.ToLower().Contains(s))
                    || (j.JobType != null && j.JobType.ToLower().Contains(s)));
            }

            if (!string.IsNullOrEmpty(location))
                query = query.Where(j => j.JobLocation != null && j.JobLocation.Contains(location));

            if (!string.IsNullOrEmpty(sector))
                query = query.Where(j => j.JobSector == sector);

            if (!string.IsNullOrEmpty(jobType))
                query = query.Where(j => j.JobType == jobType);

            if (salaryMin.HasValue && salaryMin > 0)
                query = query.Where(j => j.SalaryMin >= salaryMin.Value);

            if (salaryMax.HasValue && salaryMax > 0)
                query = query.Where(j => j.SalaryMax <= salaryMax.Value);

            if (!string.IsNullOrEmpty(experience))
            {
                if (int.TryParse(experience, out int expYears))
                {
                    query = query.Where(j => j.MinimumExperience >= expYears);
                }
            }

            if (!string.IsNullOrEmpty(industry))
            {
                string industryLower = industry.ToLower();
                query = query.Where(j => 
                    (j.JobDescription != null && j.JobDescription.ToLower().Contains(industryLower)) ||
                    (j.RequiredSkills != null && j.RequiredSkills.ToLower().Contains(industryLower)));
            }

            if (!string.IsNullOrEmpty(datePosted))
            {
                DateTime? sinceDate = datePosted.ToLower() switch
                {
                    "24h" => DateTime.Now.AddHours(-24),
                    "7d" => DateTime.Now.AddDays(-7),
                    "14d" => DateTime.Now.AddDays(-14),
                    "30d" => DateTime.Now.AddDays(-30),
                    _ => int.TryParse(datePosted, out int days) ? DateTime.Now.AddDays(-days) : null
                };

                if (sinceDate.HasValue)
                {
                    query = query.Where(j => j.CreatedDate >= sinceDate.Value);
                }
            }

            return await query.OrderByDescending(j => j.CreatedDate).ToListAsync();
        }

        public async Task<Job?> GetJobByIdAsync(int jobId)
            => await _db.Jobs
                .TagWith("JobService.GetJobByIdAsync")
                .Include(j => j.Recruiter).ThenInclude(r => r.User)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.JobID == jobId);

        public async Task<List<Job>> GetJobsByRecruiterAsync(int recruiterId)
            => await _db.Jobs
                .TagWith("JobService.GetJobsByRecruiterAsync")
                .Include(j => j.Company)
                .Include(j => j.Applications)
                .Where(j => j.RecruiterID == recruiterId)
                .OrderByDescending(j => j.CreatedDate)
                .ToListAsync();

        public async Task<(bool success, string message, int jobId)> CreateJobAsync(JobCreateDTO dto, int recruiterId)
        {
            var recruiter = await _db.Recruiters.FindAsync(recruiterId);
            if (recruiter == null) return (false, "Recruiter not found.", 0);

            // Validate ApplicationDeadline
            if (dto.ApplicationDeadline.HasValue && dto.ApplicationDeadline.Value.Date < DateTime.Today.AddDays(1))
            {
                return (false, "Application deadline must be at least 1 day in the future.", 0);
            }

            string? attachmentPath = null;
            if (dto.Attachment != null && dto.Attachment.Length > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "jobs");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Attachment.FileName)}";
                var fullPath = Path.Combine(folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                await dto.Attachment.CopyToAsync(stream);
                attachmentPath = $"/uploads/jobs/{fileName}";
            }

            var job = new Job
            {
                RecruiterID = recruiterId,
                CompanyID = recruiter?.CompanyID,
                JobTitle = dto.JobTitle,
                JobDescription = dto.JobDescription,
                RequiredSkills = dto.RequiredSkills,
                MinimumExperience = dto.MinimumExperience,
                EducationLevel = dto.EducationLevel,
                JobLocation = dto.JobLocation,
                SalaryMin = dto.SalaryMin,
                SalaryMax = dto.SalaryMax,
                JobType = dto.JobType,
                JobSector = dto.JobSector,
                ApplicationDeadline = dto.ApplicationDeadline,
                AttachmentPath = attachmentPath,
                JobStatus = "Open"
            };

            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();
            return (true, "Job posted successfully.", job.JobID);
        }

        public async Task<(bool success, string message)> UpdateJobAsync(int jobId, JobCreateDTO dto)
        {
            var job = await _db.Jobs.FindAsync(jobId);
            if (job == null) return (false, "Job not found.");

            // Validate ApplicationDeadline
            if (dto.ApplicationDeadline.HasValue && dto.ApplicationDeadline.Value.Date < DateTime.Today.AddDays(1))
            {
                return (false, "Application deadline must be at least 1 day in the future.");
            }

            if (dto.Attachment != null && dto.Attachment.Length > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "jobs");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Attachment.FileName)}";
                var fullPath = Path.Combine(folder, fileName);
                using var stream = new FileStream(fullPath, FileMode.Create);
                await dto.Attachment.CopyToAsync(stream);
                job.AttachmentPath = $"/uploads/jobs/{fileName}";
            }

            job.JobTitle = dto.JobTitle;
            job.JobDescription = dto.JobDescription;
            job.RequiredSkills = dto.RequiredSkills;
            job.MinimumExperience = dto.MinimumExperience;
            job.EducationLevel = dto.EducationLevel;
            job.JobLocation = dto.JobLocation;
            job.SalaryMin = dto.SalaryMin;
            job.SalaryMax = dto.SalaryMax;
            job.JobType = dto.JobType;
            job.JobSector = dto.JobSector;
            job.ApplicationDeadline = dto.ApplicationDeadline;
            job.UpdatedDate = DateTime.Now;

            // Sync JobSkills junction table
            var existingSkills = await _db.JobSkills.Where(js => js.JobID == jobId).ToListAsync();
            _db.JobSkills.RemoveRange(existingSkills);
            
            if (!string.IsNullOrEmpty(dto.RequiredSkills))
            {
                var skillNames = dto.RequiredSkills.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var name in skillNames.Select(s => s.Trim()).Distinct())
                {
                    var skill = await _db.Skills.FirstOrDefaultAsync(s => s.SkillName == name)
                               ?? new Skill { SkillName = name, SkillCategory = "General" };
                    if (skill.SkillID == 0) _db.Skills.Add(skill);

                    _db.JobSkills.Add(new JobSkill { JobID = job.JobID, SkillID = skill.SkillID });
                }
            }

            await _db.SaveChangesAsync();
            return (true, "Job updated successfully.");
        }

        public async Task<(bool success, string message)> CloseJobAsync(int jobId)
        {
            var job = await _db.Jobs.FindAsync(jobId);
            if (job == null) return (false, "Job not found.");
            job.JobStatus = "Closed";
            job.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();
            return (true, "Job closed.");
        }

        public async Task<List<Job>> GetSavedJobsAsync(int applicantId)
        {
            return await _db.SavedJobs
                .Where(sj => sj.ApplicantID == applicantId)
                .Include(sj => sj.Job)
                .ThenInclude(j => j.Company)
                .Select(sj => sj.Job)
                .ToListAsync();
        }

        public async Task<(bool success, string message)> SaveJobAsync(int applicantId, int jobId)
        {
            var exists = await _db.SavedJobs.AnyAsync(sj => sj.ApplicantID == applicantId && sj.JobID == jobId);
            if (exists) return (false, "Job is already saved.");

            var job = await _db.Jobs.FindAsync(jobId);
            if (job == null) return (false, "Job not found.");

            _db.SavedJobs.Add(new SavedJob { ApplicantID = applicantId, JobID = jobId, SavedDate = DateTime.Now });
            await _db.SaveChangesAsync();
            return (true, "Job saved successfully.");
        }

        public async Task<(bool success, string message)> UnsaveJobAsync(int applicantId, int jobId)
        {
            var savedJob = await _db.SavedJobs.FirstOrDefaultAsync(sj => sj.ApplicantID == applicantId && sj.JobID == jobId);
            if (savedJob == null) return (false, "Job was not saved.");

            _db.SavedJobs.Remove(savedJob);
            await _db.SaveChangesAsync();
            return (true, "Job removed from saved list.");
        }
    }
}

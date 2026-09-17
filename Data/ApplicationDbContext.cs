using Microsoft.EntityFrameworkCore;
using AIResumeScreeningSystem.Models;

namespace AIResumeScreeningSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── Core ──
        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }

        // ── Jobs ──
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<JobSkill> JobSkills { get; set; }
        public DbSet<ApplicantSkill> ApplicantSkills { get; set; }

        // ── Recruitment Workflow ──
        public DbSet<Application> Applications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
        public DbSet<AdminEarning> AdminEarnings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ────────────────────────────────────────
            // UNIQUE CONSTRAINTS
            // ────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.CompanyName)
                .IsUnique();

            modelBuilder.Entity<Application>()
                .HasIndex(a => a.ApplicationTrackingID)
                .IsUnique();

            // ────────────────────────────────────────
            // USER → APPLICANT (1:1)
            // ────────────────────────────────────────
            modelBuilder.Entity<Applicant>()
                .HasOne(a => a.User)
                .WithOne(u => u.Applicant)
                .HasForeignKey<Applicant>(a => a.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // ────────────────────────────────────────
            // USER → RECRUITER (1:1)
            // ────────────────────────────────────────
            modelBuilder.Entity<Recruiter>()
                .HasOne(r => r.User)
                .WithOne(u => u.Recruiter)
                .HasForeignKey<Recruiter>(r => r.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // ────────────────────────────────────────
            // COMPANY → RECRUITERS (1:N)
            // ────────────────────────────────────────
            modelBuilder.Entity<Recruiter>()
                .HasOne(r => r.Company)
                .WithMany(c => c.Recruiters)
                .HasForeignKey(r => r.CompanyID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ────────────────────────────────────────
            // COMPANY → JOBS (1:N)
            // ────────────────────────────────────────
            modelBuilder.Entity<Job>()
                .HasOne(j => j.Company)
                .WithMany(c => c.Jobs)
                .HasForeignKey(j => j.CompanyID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ────────────────────────────────────────
            // RECRUITER → JOBS (1:N)
            // ────────────────────────────────────────
            modelBuilder.Entity<Job>()
                .HasOne(j => j.Recruiter)
                .WithMany(r => r.Jobs)
                .HasForeignKey(j => j.RecruiterID)
                .OnDelete(DeleteBehavior.Restrict);

            // ────────────────────────────────────────
            // APPLICATION → APPLICANT (N:1)
            // ────────────────────────────────────────
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Applicant)
                .WithMany(ap => ap.Applications)
                .HasForeignKey(a => a.ApplicantID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // ────────────────────────────────────────
            // APPLICATION → JOB (N:1)
            // ────────────────────────────────────────
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Job)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobID)
                .OnDelete(DeleteBehavior.Restrict);

            // ────────────────────────────────────────
            // APPLICANT SKILL (junction: Applicant ↔ Skill)
            // ────────────────────────────────────────
            modelBuilder.Entity<ApplicantSkill>()
                .HasOne(x => x.Applicant)
                .WithMany(a => a.ApplicantSkills)
                .HasForeignKey(x => x.ApplicantID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicantSkill>()
                .HasOne(x => x.Skill)
                .WithMany(s => s.ApplicantSkills)
                .HasForeignKey(x => x.SkillID)
                .OnDelete(DeleteBehavior.Restrict);

            // ────────────────────────────────────────
            // JOB SKILL (junction: Job ↔ Skill)
            // ────────────────────────────────────────
            modelBuilder.Entity<JobSkill>()
                .HasOne(x => x.Job)
                .WithMany(j => j.JobSkills)
                .HasForeignKey(x => x.JobID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JobSkill>()
                .HasOne(x => x.Skill)
                .WithMany(s => s.JobSkills)
                .HasForeignKey(x => x.SkillID)
                .OnDelete(DeleteBehavior.Restrict);

            // ────────────────────────────────────────
            // INTERVIEW → APPLICATION (N:1)
            // ────────────────────────────────────────
            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Application)
                .WithMany(a => a.Interviews)
                .HasForeignKey(i => i.ApplicationID)
                .OnDelete(DeleteBehavior.Cascade);

            // ────────────────────────────────────────
            // SEED DATA
            // ────────────────────────────────────────

            var users = new List<User>
            {
                new User { UserID = 1, FullName = "System Admin", Email = "admin@janala.com", PasswordHash = "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", Role = "Admin", AccountStatus = "Active", EmailVerified = true, CreatedDate = new DateTime(2026, 1, 1), UpdatedDate = new DateTime(2026, 1, 1), IsFirstLogin = true },
                new User { UserID = 2, FullName = "Main Applicant", Email = "applicant@janala.com", PasswordHash = "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", Role = "Applicant", AccountStatus = "Active", EmailVerified = true, CreatedDate = new DateTime(2026, 1, 1), UpdatedDate = new DateTime(2026, 1, 1), IsFirstLogin = true }
            };

            for (int i = 1; i <= 10; i++)
            {
                users.Add(new User { UserID = 2 + i, FullName = $"Recruiter {i}", Email = $"recruiter{i}@janala.com", PasswordHash = "$2a$11$VEcFQhjlEFKhK/10UGhn3ubUqgorau4zWrIE2nzKTSo3tkBZJXkSm", Role = "Recruiter", AccountStatus = "Active", EmailVerified = true, CreatedDate = new DateTime(2026, 1, 1), UpdatedDate = new DateTime(2026, 1, 1), IsFirstLogin = true });
            }

            modelBuilder.Entity<User>().HasData(users);

            modelBuilder.Entity<Company>().HasData(
                new Company { CompanyID = 1, CompanyName = "TechCorp Solutions", CompanyWebsite = "https://techcorp.example.com", CompanyLocation = "Dhaka, Bangladesh", CompanySize = "201-500", IndustryType = "Information Technology", CompanyStatus = "Active", CreatedDate = new DateTime(2026, 1, 1) }
            );

            var recruiters = new List<Recruiter>();
            for (int i = 1; i <= 10; i++)
            {
                recruiters.Add(new Recruiter { RecruiterID = i, UserID = 2 + i, CompanyID = 1, Designation = $"Technical Recruiter {i}", RecruiterStatus = "Active", CreatedDate = new DateTime(2026, 1, 1) });
            }
            modelBuilder.Entity<Recruiter>().HasData(recruiters);

            modelBuilder.Entity<Applicant>().HasData(
                new Applicant { ApplicantID = 1, UserID = 2, YearsOfExperience = 3, ProfileCompletionPercent = 0, CreatedDate = new DateTime(2026, 1, 1) }
            );

            modelBuilder.Entity<Skill>().HasData(
                new Skill { SkillID = 1, SkillName = "C#", SkillCategory = "Programming", CreatedDate = new DateTime(2026, 1, 1) },
                new Skill { SkillID = 2, SkillName = "ASP.NET Core", SkillCategory = "Framework", CreatedDate = new DateTime(2026, 1, 1) },
                new Skill { SkillID = 3, SkillName = "SQL Server", SkillCategory = "Database", CreatedDate = new DateTime(2026, 1, 1) }
            );

            var jobs = new List<Job>();
            string[] titles = { 
                "Senior .NET Developer", "Frontend Engineer (React)", "SQL Database Admin", 
                "DevOps Specialist", "Product Manager", "UI/UX Designer", 
                "QA Automation Engineer", "Machine Learning Engineer", "Cloud Architect", "Full Stack Developer" 
            };
            string[] locations = { "Dhaka", "Chittagong", "Sylhet", "Remote", "Dhaka", "Dhaka", "Remote", "Dhaka", "Chittagong", "Remote" };

            for (int i = 1; i <= 10; i++)
            {
                jobs.Add(new Job 
                { 
                    JobID = i, 
                    RecruiterID = i, 
                    CompanyID = 1, 
                    JobTitle = titles[i-1], 
                    JobDescription = $"Exciting opportunity for a {titles[i-1]} at TechCorp.", 
                    RequiredSkills = "C#, ASP.NET Core, SQL Server", 
                    MinimumExperience = 2 + i % 5, 
                    EducationLevel = "BSc", 
                    JobLocation = locations[i-1], 
                    SalaryMin = 50000 + (i * 5000), 
                    SalaryMax = 80000 + (i * 8000), 
                    JobType = i % 2 == 0 ? "Full Time" : "Contract", 
                    JobStatus = "Open", 
                    CreatedDate = new DateTime(2026, 1, 1), 
                    UpdatedDate = new DateTime(2026, 1, 1),
                    AttachmentPath = "/uploads/jobs/sample_circular.pdf"
                });
            }
            modelBuilder.Entity<Job>().HasData(jobs);

            // ────────────────────────────────────────
            // EXPLICIT TABLE NAMES
            // ────────────────────────────────────────
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Company>().ToTable("Companies");
            modelBuilder.Entity<Applicant>().ToTable("Applicants");
            modelBuilder.Entity<Recruiter>().ToTable("Recruiters");
            modelBuilder.Entity<Job>().ToTable("Jobs");
            modelBuilder.Entity<Skill>().ToTable("Skills");
            modelBuilder.Entity<JobSkill>().ToTable("JobSkills");
            modelBuilder.Entity<ApplicantSkill>().ToTable("ApplicantSkills");
            modelBuilder.Entity<Application>().ToTable("Applications");
            modelBuilder.Entity<Interview>().ToTable("Interviews");

            // ────────────────────────────────────────
            // SAVED JOBS (junction: Applicant ↔ Job)
            // ────────────────────────────────────────
            modelBuilder.Entity<SavedJob>()
                .ToTable("SavedJobs")
                .HasOne(sj => sj.Applicant)
                .WithMany(a => a.SavedJobs)
                .HasForeignKey(sj => sj.ApplicantID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedJob>()
                .HasOne(sj => sj.Job)
                .WithMany()
                .HasForeignKey(sj => sj.JobID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

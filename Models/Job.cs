using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.Models
{
    public class Job
    {
        [Key]
        public int JobID { get; set; }

        [ForeignKey("Recruiter")]
        public int RecruiterID { get; set; }
        public Recruiter Recruiter { get; set; } = null!;

        // Optional link to Company
        [ForeignKey("Company")]
        public int? CompanyID { get; set; }
        public Company? Company { get; set; }

        [Required, MaxLength(200)]
        public string JobTitle { get; set; } = string.Empty;

        public string JobDescription { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;

        public int MinimumExperience { get; set; } = 0;

        [MaxLength(100)]
        public string? EducationLevel { get; set; }

        [MaxLength(200)]
        public string? JobLocation { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryMin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryMax { get; set; }

        [MaxLength(50)]
        public string JobType { get; set; } = "Full Time";

        [MaxLength(20)]
        public string JobSector { get; set; } = "Private"; // Govt, Private

        public int? JobCategoryID { get; set; }
        public DateTime? ApplicationDeadline { get; set; }

        [MaxLength(500)]
        public string? AttachmentPath { get; set; }

        [MaxLength(20)]
        public string JobStatus { get; set; } = "Open"; // Open, Closed

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
    }
}

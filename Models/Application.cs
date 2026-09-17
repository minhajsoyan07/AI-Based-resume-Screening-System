using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.Models
{
    public class Application
    {
        [Key]
        public int ApplicationID { get; set; }

        [ForeignKey("Applicant")]
        public int? ApplicantID { get; set; }
        public Applicant? Applicant { get; set; } = null!;

        [ForeignKey("Job")]
        public int JobID { get; set; }
        public Job Job { get; set; } = null!;

        [MaxLength(500)]
        public string? ResumeFilePath { get; set; }

        [MaxLength(100)]
        public string? ApplicationTrackingID { get; set; }


        [Column(TypeName = "decimal(5,2)")]
        public decimal MatchScore { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal SkillScore { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal ExperienceScore { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal EducationScore { get; set; } = 0;

        [MaxLength(30)]
        public string ApplicationStatus { get; set; } = "Applied";
        // Applied, UnderReview, Shortlisted, Rejected, InterviewScheduled,
        // InterviewCompleted, OfferSent, Hired, Withdrawn

        public DateTime AppliedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Interview> Interviews { get; set; } = new List<Interview>();

        public string? Notes { get; set; }

        [MaxLength(200)]
        public string? MatchVerdict { get; set; }

        public string? MissingRequirements { get; set; }

    }
}

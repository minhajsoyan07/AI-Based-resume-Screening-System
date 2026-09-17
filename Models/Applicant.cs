using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.Models
{
    public class Applicant
    {
        [Key]
        public int ApplicantID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public User User = null!;

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(150)]
        public string? CurrentLocation { get; set; }

        [MaxLength(300)]
        public string? LinkedInURL { get; set; }

        [MaxLength(300)]
        public string? PortfolioURL { get; set; }

        // Address from CV parsing
        [MaxLength(250)]
        public string? Address { get; set; }

        public int YearsOfExperience { get; set; } = 0;

        [MaxLength(100)]
        public string? HighestEducation { get; set; }

        [MaxLength(150)]
        public string? CurrentJobTitle { get; set; }

        public string? ProfileSummary { get; set; }

        [MaxLength(500)]
        public string? Certifications { get; set; } // Comma-separated

        [MaxLength(500)]
        public string? Languages { get; set; } // Comma-separated

        public string? Projects { get; set; } // Larger text block, newline-separated

        public string? Achievements { get; set; } // Larger text block, newline-separated

        [MaxLength(500)]
        public string? ResumeFilePath { get; set; }

        public int ProfileCompletionPercent { get; set; } = 0;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Tracks last login for activity-based ranking boost (premium engagement advantage).
        /// </summary>
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Tracks last meaningful activity (profile update, application, etc.) for ranking.
        /// </summary>
        public DateTime? LastActivityDate { get; set; }

        // Settings & Preferences
        public bool ProfileVisibility { get; set; } = true;
        public bool DataSharingConsent { get; set; } = true;
        
        [MaxLength(20)]
        public string EmailFrequency { get; set; } = "Daily"; // Realtime, Daily, Weekly, None
        
        public bool InAppNotifications { get; set; } = true;
        
        [MaxLength(20)]
        public string SearchStatus { get; set; } = "active"; // active, passive, closed

        // Navigation
        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public ICollection<ApplicantSkill> ApplicantSkills { get; set; } = new List<ApplicantSkill>();
        public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    }
}

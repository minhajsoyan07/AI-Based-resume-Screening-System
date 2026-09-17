using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; } = "Applicant"; // Admin, Recruiter, Applicant

        [MaxLength(20)]
        public string AccountStatus { get; set; } = "Active"; // Active, Disabled

        [MaxLength(500)]
        public string? ProfilePicturePath { get; set; }

        public bool EmailVerified { get; set; } = false;
        public DateTime? LastLoginDate { get; set; }
        
        [MaxLength(100)]
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Credit System
        public int Credits { get; set; } = 0;
        public bool IsFirstLogin { get; set; } = true;

        // Navigation
        public Applicant? Applicant { get; set; }
        public Recruiter? Recruiter { get; set; }
    }
}

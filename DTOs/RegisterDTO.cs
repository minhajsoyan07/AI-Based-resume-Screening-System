using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using AIResumeScreeningSystem.Attributes;

namespace AIResumeScreeningSystem.DTOs
{
    public class RegisterDTO
    {
        [Required, MaxLength(150)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "User name can only contain letters and numbers (no spaces or special characters)")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = "Applicant";
    }

    public class JobSeekerRegisterDTO
    {
        [Required(ErrorMessage = "User name is required")]
        [MaxLength(150)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "User name can only contain letters and numbers (no spaces or special characters)")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required")]
        [MaxLength(30)]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Enter valid 11-digit BD number (e.g. 017...)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Maximum file size is 5 MB")]
        [AllowedExtensions(new[] { ".pdf", ".docx", ".txt", ".jpg", ".jpeg", ".png" }, ErrorMessage = "Allowed formats: PDF, DOCX, TXT, JPG, PNG")]
        public IFormFile? ResumeFile { get; set; }

        // --- Parsed Review Fields ---
        public string? HighestEducation { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? CurrentJobTitle { get; set; }
        public string? ProfileSummary { get; set; }
        public string? LinkedInURL { get; set; }
        public string? GitHubURL { get; set; }
        public string? ExtractedSkills { get; set; } // Comma separated for editing
    }

    public class RecruiterRegisterDTO
    {
        [Required(ErrorMessage = "User name is required")]
        [MaxLength(150)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "User name can only contain letters and numbers (no spaces or special characters)")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person's name is required")]
        [MaxLength(150)]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required")]
        [MaxLength(30)]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Enter valid 11-digit BD number (e.g. 017...)")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? CompanyNameBangla { get; set; }

        [Required(ErrorMessage = "Organization category is required")]
        [MaxLength(100)]
        public string OrganizationCategory { get; set; } = string.Empty;

        [Required(ErrorMessage = "Industry type is required")]
        [MaxLength(100)]
        public string IndustryType { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? CompanyWebsite { get; set; }

        [MaxLength(500)]
        public string? Tags { get; set; }

        [MaxLength(100)]
        public string? Tagline { get; set; }

        public IFormFile? LogoFile { get; set; }
        public IFormFile? CoverPhotoFile { get; set; }
        public IFormFile? SignatureFile { get; set; }

        public int? EstablishmentYear { get; set; }

        [MaxLength(50)]
        public string? CompanySize { get; set; }

        [Required(ErrorMessage = "Organization email is required")]
        [EmailAddress(ErrorMessage = "Invalid organization email")]
        [MaxLength(150)]
        public string OrganizationEmail { get; set; } = string.Empty;

        public string? AboutOrganization { get; set; }
        public string? Vision { get; set; }
        public string? Mission { get; set; }

        [MaxLength(100)]
        public string? Division { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        [MaxLength(100)]
        public string? Thana { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(500)]
        public string? FullAddress { get; set; }

        [MaxLength(500)]
        public string? FullAddressBangla { get; set; }

        [MaxLength(20)]
        public string? OrganizationMobile { get; set; }

        [MaxLength(30)]
        public string? LandPhoneNumber { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        [MaxLength(100)]
        public string? Designation { get; set; }
    }
}
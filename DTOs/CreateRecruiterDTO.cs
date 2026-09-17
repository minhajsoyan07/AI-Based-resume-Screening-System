using System.ComponentModel.DataAnnotations;

namespace AIResumeScreeningSystem.DTOs
{
    public class CreateRecruiterDTO
    {
        [Required, MaxLength(150)]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Full name can only contain letters, spaces, hyphens, and apostrophes")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? CompanyName { get; set; }

        [MaxLength(100)]
        public string? Designation { get; set; }

        public int? CompanyID { get; set; }
    }
}

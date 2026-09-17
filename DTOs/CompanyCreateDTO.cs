using System.ComponentModel.DataAnnotations;

namespace AIResumeScreeningSystem.DTOs
{
    public class CompanyCreateDTO
    {
        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? CompanyWebsite { get; set; }

        [MaxLength(200)]
        public string? CompanyLocation { get; set; }

        [MaxLength(50)]
        public string? CompanySize { get; set; }

        [MaxLength(100)]
        public string? IndustryType { get; set; }

        public string? Description { get; set; }
    }
}

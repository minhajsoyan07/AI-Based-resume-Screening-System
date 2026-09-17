using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.Models
{
    public class Company
    {
        [Key]
        public int CompanyID { get; set; }

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? CompanyWebsite { get; set; }

        [MaxLength(200)]
        public string? CompanyLocation { get; set; }

        [MaxLength(50)]
        public string? CompanySize { get; set; } // 1-10, 11-50, 51-200, 201-500, 500+

        [MaxLength(100)]
        public string? IndustryType { get; set; }

        [MaxLength(500)]
        public string? LogoPath { get; set; }

        [MaxLength(200)]
        public string? CompanyNameBangla { get; set; }

        [MaxLength(100)]
        public string? OrganizationCategory { get; set; }

        [MaxLength(100)]
        public string? Tagline { get; set; }

        [MaxLength(500)]
        public string? Tags { get; set; }

        [MaxLength(500)]
        public string? CoverPhotoPath { get; set; }

        [MaxLength(500)]
        public string? SignaturePath { get; set; }

        public int? EstablishmentYear { get; set; }

        [MaxLength(150)]
        public string? OrganizationEmail { get; set; }

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
        public string? MobileNumber { get; set; }

        [MaxLength(30)]
        public string? LandPhoneNumber { get; set; }

        public string? Description { get; set; }
        public string? AboutOrganization { get; set; }

        [MaxLength(20)]
        public string CompanyStatus { get; set; } = "Active"; // Active, Inactive

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Recruiter> Recruiters { get; set; } = new List<Recruiter>();
        public ICollection<Job> Jobs { get; set; } = new List<Job>();
    }
}

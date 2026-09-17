using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AIResumeScreeningSystem.DTOs
{
    public class CompanySetupDTO
    {
        [Required(ErrorMessage = "Name of the organization is required.")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? CompanyNameBangla { get; set; }

        [Required(ErrorMessage = "Organization category is required.")]
        [MaxLength(100)]
        public string OrganizationCategory { get; set; } = string.Empty;

        [Required(ErrorMessage = "Industry Type is required.")]
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

        [Required(ErrorMessage = "Organization email is required.")]
        [EmailAddress]
        [MaxLength(150)]
        public string OrganizationEmail { get; set; } = string.Empty;

        public string? AboutOrganization { get; set; }
        public string? Vision { get; set; }
        public string? Mission { get; set; }

        // Address & Contact
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
    }
}

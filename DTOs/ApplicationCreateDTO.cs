using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AIResumeScreeningSystem.DTOs
{
    public class ApplicationCreateDTO
    {
        [Required]
        public int JobID { get; set; }

        [DataType(DataType.Upload)]
        [AIResumeScreeningSystem.Attributes.MaxFileSize(5 * 1024 * 1024)] // 5MB
        [AIResumeScreeningSystem.Attributes.AllowedExtensions(new string[] { ".pdf", ".docx" })]
        [AIResumeScreeningSystem.Attributes.FileSignature]
        public IFormFile? ResumeFile { get; set; }
        
        public bool UseExistingResume { get; set; }

        public string? CoverLetter { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AIResumeScreeningSystem.DTOs
{
    public class JobCreateDTO
    {
        [Required, MaxLength(200)]
        public string JobTitle { get; set; } = string.Empty;

        [Required]
        public string JobDescription { get; set; } = string.Empty;

        [Required]
        public string RequiredSkills { get; set; } = string.Empty;

        public int MinimumExperience { get; set; } = 0;

        [MaxLength(100)]
        public string? EducationLevel { get; set; }

        [MaxLength(200)]
        public string? JobLocation { get; set; }

        public decimal SalaryMin { get; set; }
        public decimal SalaryMax { get; set; }

        [MaxLength(50)]
        public string JobType { get; set; } = "Full Time";
        
        [MaxLength(20)]
        public string JobSector { get; set; } = "Private";

        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "Application deadline must be a future date")]
        public DateTime? ApplicationDeadline { get; set; }

        public double SkillMatchThreshold { get; set; } = 75.0;

        [AIResumeScreeningSystem.Attributes.MaxFileSize(10 * 1024 * 1024)] // 10MB
        [AIResumeScreeningSystem.Attributes.FileSignature]
        public IFormFile? Attachment { get; set; }
        public string? AttachmentPath { get; set; }
    }

    /// <summary>
    /// Custom validation attribute to ensure ApplicationDeadline is in the future
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.IClientModelValidator
    {
        private const int MinimumDaysInFuture = 1;

        public override bool IsValid(object? value)
        {
            if (value == null) return true; // Null is allowed (optional field)

            if (value is DateTime dateTime)
            {
                // Allow only dates that are at least MinimumDaysInFuture days from today
                var minDate = DateTime.Today.AddDays(MinimumDaysInFuture);
                
                if (dateTime.Date < minDate)
                {
                    ErrorMessage = $"Application deadline must be at least {MinimumDaysInFuture} day(s) in the future";
                    return false;
                }
                return true;
            }

            return false;
        }

        public void AddValidation(Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ClientModelValidationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.TryAdd("data-val-futuredate", ErrorMessage ?? $"Application deadline must be at least {MinimumDaysInFuture} day(s) in the future");
            var minDate = DateTime.Today.AddDays(MinimumDaysInFuture).ToString("yyyy-MM-dd");
            context.Attributes.TryAdd("data-val-futuredate-mindate", minDate);
        }
    }
}

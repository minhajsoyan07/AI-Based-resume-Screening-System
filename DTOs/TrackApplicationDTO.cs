using System.ComponentModel.DataAnnotations;

namespace AIResumeScreeningSystem.DTOs
{
    public class TrackApplicationDTO
    {
        [Required]
        public string ApplicationTrackingID { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}

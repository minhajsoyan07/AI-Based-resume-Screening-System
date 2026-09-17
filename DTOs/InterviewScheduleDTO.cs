using System.ComponentModel.DataAnnotations;

namespace AIResumeScreeningSystem.DTOs
{
    public class InterviewScheduleDTO
    {
        [Required]
        public int ApplicationID { get; set; }

        [Required]
        public DateTime InterviewDate { get; set; }

        [Required]
        public string InterviewTime { get; set; } = string.Empty;

        [Required]
        public string InterviewType { get; set; } = "Online";

        public string? MeetingLink { get; set; }
        public string? InterviewLocation { get; set; }
        public int InterviewRound { get; set; } = 1;
        public string? Notes { get; set; }
    }
}

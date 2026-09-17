using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIResumeScreeningSystem.Models
{
    public class Interview
    {
        [Key]
        public int InterviewID { get; set; }

        [ForeignKey("Application")]
        public int ApplicationID { get; set; }
        public Application Application { get; set; } = null!;

        public int InterviewRound { get; set; } = 1;
        public DateTime InterviewDate { get; set; }
        public TimeSpan InterviewTime { get; set; }

        [MaxLength(50)]
        public string InterviewType { get; set; } = "Online"; // Online, Offline

        [MaxLength(300)]
        public string? MeetingLink { get; set; }

        [MaxLength(300)]
        public string? InterviewLocation { get; set; }

        [MaxLength(30)]
        public string InterviewStatus { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled

        [MaxLength(30)]
        public string? InterviewResult { get; set; } // Passed, Failed, Pending

        public string? Notes { get; set; }
        public string? Feedback { get; set; }
    }
}

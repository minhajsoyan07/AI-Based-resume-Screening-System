using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIResumeScreeningSystem.Models
{
    public class SavedJob
    {
        [Key]
        public int SavedJobID { get; set; }

        [Required]
        public int ApplicantID { get; set; }
        [ForeignKey("ApplicantID")]
        public Applicant Applicant { get; set; } = null!;

        [Required]
        public int JobID { get; set; }
        [ForeignKey("JobID")]
        public Job Job { get; set; } = null!;

        public DateTime SavedDate { get; set; } = DateTime.Now;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIResumeScreeningSystem.Models
{
    public class ApplicantSkill
    {
        [Key]
        public int ApplicantSkillID { get; set; }

        [ForeignKey("Applicant")]
        public int ApplicantID { get; set; }
        public Applicant Applicant { get; set; } = null!;

        [ForeignKey("Skill")]
        public int SkillID { get; set; }
        public Skill Skill { get; set; } = null!;

        [MaxLength(50)]
        public string? SkillLevel { get; set; } // Beginner, Intermediate, Advanced
    }
}

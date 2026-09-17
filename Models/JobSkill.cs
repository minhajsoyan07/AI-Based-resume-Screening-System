using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIResumeScreeningSystem.Models
{
    public class JobSkill
    {
        [Key]
        public int JobSkillID { get; set; }

        [ForeignKey("Job")]
        public int JobID { get; set; }
        public Job Job { get; set; } = null!;

        [ForeignKey("Skill")]
        public int SkillID { get; set; }
        public Skill Skill { get; set; } = null!;

        [MaxLength(50)]
        public string? ImportanceLevel { get; set; } // Required, Preferred, Nice-to-have
    }
}

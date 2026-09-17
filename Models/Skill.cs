using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.Models
{
    public class Skill
    {
        [Key]
        public int SkillID { get; set; }

        [Required, MaxLength(100)]
        public string SkillName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SkillCategory { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<ApplicantSkill> ApplicantSkills { get; set; } = new List<ApplicantSkill>();
        public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
    }
}

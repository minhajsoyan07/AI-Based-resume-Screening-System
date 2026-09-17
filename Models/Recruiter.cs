using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.Models
{
    public class Recruiter
    {
        [Key]
        public int RecruiterID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public User User { get; set; } = null!;

        // Link to Company (nullable for backward compat)
        [ForeignKey("Company")]
        public int? CompanyID { get; set; }
        public Company? Company { get; set; }

        [MaxLength(200)]
        public string? CompanyName { get; set; }

        [MaxLength(300)]
        public string? CompanyWebsite { get; set; }

        [MaxLength(200)]
        public string? CompanyLocation { get; set; }

        [MaxLength(50)]
        public string? CompanySize { get; set; }

        [MaxLength(100)]
        public string? IndustryType { get; set; }

        [MaxLength(100)]
        public string? Designation { get; set; }

        [MaxLength(20)]
        public string RecruiterStatus { get; set; } = "Active";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Job> Jobs { get; set; } = new List<Job>();
    }
}

using System;
using System.Collections.Generic;

namespace AIResumeScreeningSystem.DTOs
{
    public class JobCardViewModel
    {
        public int JobID { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string? Location { get; set; }
        public string? JobType { get; set; }
        public decimal SalaryMin { get; set; }
        public decimal SalaryMax { get; set; }
        public int MinimumExperience { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public double? MatchScore { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public bool IsRecommended { get; set; }
        public string? AttachmentPath { get; set; }
        public bool IsBookmarked { get; set; }
    }
}

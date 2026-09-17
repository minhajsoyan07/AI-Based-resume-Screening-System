namespace AIResumeScreeningSystem.DTOs
{
    public class CandidateViewModel
    {
        public int ApplicationID { get; set; }
        public int? ApplicantID { get; set; }
        public int JobID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal MatchScore { get; set; }
        public decimal SkillScore { get; set; }
        public decimal ExperienceScore { get; set; }
        public decimal EducationScore { get; set; }
        public decimal KeywordScore { get; set; }
        public decimal CertificationScore { get; set; }
        public int RankedPosition { get; set; }
        public string ApplicationStatus { get; set; } = string.Empty;
        public string? ResumeFilePath { get; set; }
        public string Skills { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public DateTime AppliedDate { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string? OfferStatus { get; set; }
        public string? Notes { get; set; }
        public string? MatchVerdict { get; set; }
        public string? MissingRequirements { get; set; }
        public string SearchStatus { get; set; } = "active";
        public bool ProfileVisibility { get; set; } = true;
    }
}

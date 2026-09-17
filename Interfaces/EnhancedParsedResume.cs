using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public class EnhancedParsedResume
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? LinkedInURL { get; set; }
        public string? PortfolioURL { get; set; }
        public string? GitHubURL { get; set; }
        public List<(string skill, double confidence)> SkillsWithConfidence { get; set; } = new();
        public List<string> PrimarySkills { get; set; } = new();
        public List<string> SecondarySkills { get; set; } = new();
        public string? Education { get; set; }
        public string? HighestEducation { get; set; }
        public int? YearsOfExperience { get; set; }
        public int? PromotionsCount { get; set; }
        public string? CurrentJobTitle { get; set; }
        public List<string> Achievements { get; set; } = new();
        public List<string> Projects { get; set; } = new();
        public List<string> Languages { get; set; } = new();
        public List<string> Certifications { get; set; } = new();
        public List<string> Industries { get; set; } = new();
        public List<string> Tools { get; set; } = new();
        public List<string> SoftSkills { get; set; } = new();
        public string? RawText { get; set; }
        public string? ProfileSummary { get; set; }
        public decimal ProfileCompletionScore { get; set; }
        public WorkExperienceAnalysis WorkExperience { get; set; } = new();
        public List<(string section, double relevance)> SectionRelevance { get; set; } = new();
    }
}

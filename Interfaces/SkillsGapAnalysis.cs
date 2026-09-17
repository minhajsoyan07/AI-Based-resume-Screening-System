using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public class SkillsGapAnalysis
    {
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public List<string> PartialMatchSkills { get; set; } = new();
        public double MatchPercentage { get; set; }
        public string GapSeverity { get; set; } = string.Empty;
    }
}

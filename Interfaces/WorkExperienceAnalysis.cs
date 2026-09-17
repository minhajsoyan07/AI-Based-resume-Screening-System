using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public class WorkExperienceAnalysis
    {
        public int TotalYears { get; set; }
        public int CompanyCount { get; set; }
        public int PromotionCount { get; set; }
        public double AverageTenure { get; set; }
        public bool HasLeadership { get; set; }
        public bool HasCareerProgression { get; set; }
        public List<string> KeyResponsibilities { get; set; } = new();
        public List<string> Achievements { get; set; } = new();
        public string CareerLevel { get; set; } = string.Empty;
        public List<(string company, string title, int years)> ExperienceTimeline { get; set; } = new();
    }
}

using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public class JobRecommendation
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public string Location { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public DateTime PostedDate { get; set; }
        public string? AttachmentPath { get; set; }
    }
}

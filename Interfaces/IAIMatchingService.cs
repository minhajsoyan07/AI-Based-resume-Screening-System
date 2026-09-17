using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public interface IAIMatchingService
    {
        Task<decimal> CalculateMatchScoreAsync(int applicationId);
        Task<List<Job>> GetRecommendedJobsForApplicantAsync(int applicantId);
        decimal CalculateSkillScore(string? jobSkills, List<string> candidateSkills, string resumeText);
        decimal CalculateExperienceScore(int requiredYears, int candidateYears, string jobDescription = "");
        decimal CalculateEducationScore(string? requiredEdu, string? candidateEdu);
        Task<SkillsGapAnalysis> AnalyzeSkillsGapAsync(int applicationId);
        Task<List<JobRecommendation>> GetJobRecommendationsAsync(int applicantId);
        Task<AIMatchResult> AnalyzeMatchAsync(string cvText, string jobText);
        Task<string> GenerateDeepAnalysisAsync(int applicationId);
    }
}

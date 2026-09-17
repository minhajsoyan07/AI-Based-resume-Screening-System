using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public interface IJobService
    {
        Task<List<Job>> GetAllJobsAsync(string? search = null, string? location = null, string? sector = null,
            string? jobType = null, decimal? salaryMin = null, decimal? salaryMax = null,
            string? experience = null, string? industry = null, string? datePosted = null);
        Task<Job?> GetJobByIdAsync(int jobId);
        Task<List<Job>> GetJobsByRecruiterAsync(int recruiterId);
        Task<(bool success, string message, int jobId)> CreateJobAsync(JobCreateDTO dto, int recruiterId);
        Task<(bool success, string message)> UpdateJobAsync(int jobId, JobCreateDTO dto);
        Task<(bool success, string message)> CloseJobAsync(int jobId);
        Task<List<Job>> GetSavedJobsAsync(int applicantId);
        Task<(bool success, string message)> SaveJobAsync(int applicantId, int jobId);
        Task<(bool success, string message)> UnsaveJobAsync(int applicantId, int jobId);
    }
}

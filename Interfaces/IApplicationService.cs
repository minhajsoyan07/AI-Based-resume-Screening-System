using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public interface IApplicationService
    {
        Task<(bool success, string message, string? trackingId)> ApplyAsync(ApplicationCreateDTO dto, int? applicantId);
        Task<List<Application>> GetByApplicantAsync(int? applicantId);
        Task<List<CandidateViewModel>> GetCandidatesByJobAsync(int jobId);
        Task<(bool success, string message)> ShortlistAsync(int applicationId);
        Task<(bool success, string message)> RejectAsync(int applicationId);
        Task<(bool success, string message)> WithdrawAsync(int applicationId, int? applicantId);
        Task<(bool success, string message)> UpdateStatusAsync(int applicationId, string status);
        Task<(bool success, string message)> BulkShortlistAsync(List<int> applicationIds);
        Task<(bool success, string message)> BulkRejectAsync(List<int> applicationIds);
        Task<(bool success, string message)> AddNoteAsync(int applicationId, string notes);
        Task<(bool success, string message)> RunAnalysisAsync(int jobId);
    }
}

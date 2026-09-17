using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public interface IInterviewService
    {
        Task<(bool success, string message)> ScheduleAsync(InterviewScheduleDTO dto);
        Task<List<Interview>> GetUpcomingAsync(int recruiterId);
        Task<(bool success, string message)> CancelAsync(int interviewId);
        Task<(bool success, string message)> CompleteAsync(int interviewId, string result, string? feedback);
    }
}

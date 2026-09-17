using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public interface IAuthService
    {
        Task<(bool success, string message, User? user)> LoginAsync(LoginDTO dto);
        Task<(bool success, string message, User? user)> JobSeekerLoginAsync(LoginDTO dto);
        Task<(bool success, string message, User? user)> RecruiterLoginAsync(LoginDTO dto);
        Task<(bool success, string message)> RegisterAsync(RegisterDTO dto);
        Task<(bool success, string message, User? user)> JobSeekerRegisterAsync(JobSeekerRegisterDTO dto);
        Task<ParsedResumeData> PreviewParseResumeAsync(string filePath);
        Task<(bool success, string message, User? user)> RecruiterRegisterAsync(RecruiterRegisterDTO dto);
        string GenerateJwtToken(User user);
        int? GetCurrentUserId(HttpContext context);
        string? GetCurrentUserRole(HttpContext context);
        Task<bool> CheckAvailabilityAsync(string type, string value);
        Task<(bool success, string message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
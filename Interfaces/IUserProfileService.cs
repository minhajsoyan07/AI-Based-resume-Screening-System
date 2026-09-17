using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public interface IUserProfileService
    {
        Task<(bool success, string message)> UpdateProfileAsync(int userId, string? fullName, string? phoneNumber, string? profilePicture);
        Task<(bool success, string message)> UploadProfilePictureAsync(int userId, IFormFile file);
        Task<string?> GetProfilePictureAsync(int userId);
        Task<User?> GetUserByIdAsync(int userId);
    }
}

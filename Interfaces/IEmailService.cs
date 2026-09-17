namespace AIResumeScreeningSystem.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendVerificationEmailAsync(string email, string fullName, string token);
        Task<bool> SendOtpEmailAsync(string email, string otp);
        Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string token);
        
        // Notification methods for Application/Recruitment process
        Task<bool> SendApplicationConfirmationAsync(string email, string fullName, string jobTitle);
        Task<bool> SendShortlistNotificationAsync(string email, string fullName, string jobTitle);
        Task<bool> SendRejectionEmailAsync(string email, string fullName, string jobTitle);
        Task<bool> SendHiredConfirmationAsync(string email, string fullName, string jobTitle);
        Task<bool> SendInterviewInvitationAsync(string email, string fullName, string jobTitle, DateTime date, TimeSpan time, string type, string? link);
    }
}

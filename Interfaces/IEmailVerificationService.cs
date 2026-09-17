namespace AIResumeScreeningSystem.Interfaces
{
    public interface IEmailVerificationService
    {
        Task<bool> IsValidEmailAsync(string email);
    }
}

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AIResumeScreeningSystem.Models;

namespace AIResumeScreeningSystem.Interfaces
{
    public interface IAIResumeService
    {
        Task<string> ParseDocumentAsync(IFormFile uploadedFile);
        Task<string> ParseDocumentByPathAsync(string physicalFilePath);
        Task<ResumeMatchResult?> AnalyzeResumeAsync(string resumeText, string jobDescription);
    }
}

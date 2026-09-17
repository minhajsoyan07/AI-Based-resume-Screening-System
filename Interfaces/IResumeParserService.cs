using AIResumeScreeningSystem.Models;
using AIResumeScreeningSystem.DTOs;

namespace AIResumeScreeningSystem.Interfaces
{
    public interface IResumeParserService
    {
        Task<ParsedResumeData> ParseResumeAsync(string filePath);
        Task<string> ExtractTextAsync(Microsoft.AspNetCore.Http.IFormFile file);
        Task<string> ExtractTextFromPathAsync(string filePath);
    }
}

using Microsoft.AspNetCore.Http;
using System;

namespace AIResumeScreeningSystem.Helpers
{
    public static class FileValidationHelper
    {
        public static bool IsPdfPageCountValid(IFormFile file, int maxPages = 5)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var doc = UglyToad.PdfPig.PdfDocument.Open(stream);
                return doc.NumberOfPages <= maxPages;
            }
            catch (Exception)
            {
                // If it fails to parse, allow it to fall through (matching original logic)
                return true; 
            }
        }
    }
}
